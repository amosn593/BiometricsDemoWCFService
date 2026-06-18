using BiometricsDemo.Models;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using BiometricsDemo.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BiometricsDemo
{
    public class WebSocketManager
    {
        private HttpListener _listener;
        private bool _isRunning;

        // Track active connections
        private readonly List<(WebSocket socket, CancellationTokenSource cts, DisposibleFingerPrintScannerService scanService)> _connections
            = new List<(WebSocket socket, CancellationTokenSource cts, DisposibleFingerPrintScannerService scanService)>();

        private readonly object _lock = new object();


        public void Start(string uriPrefix = "http://localhost:9191/")
        {
            
            if (_isRunning) return;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(uriPrefix);
                _listener.Start();
                _isRunning = true;

                Console.WriteLine("WebSocket server started at " + uriPrefix);
                Task.Run(AcceptLoop);
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine("Failed to start WebSocket server. Did you run netsh urlacl?", ex);
                throw; // fail startup → service manager will restart it
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error starting WebSocket server", ex);
                throw;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {

                //Cancel and cleanup all connections
                lock (_lock)
                {
                    foreach (var (socket, cts, scanService) in _connections.ToList())
                    {
                        try
                        {
                            cts?.Cancel();
                            scanService?.Dispose();
                            if (socket.State == WebSocketState.Open)
                            {
                                socket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                                    "Server stopping", CancellationToken.None).Wait();
                            }
                            socket?.Dispose();
                            cts?.Dispose();
                        }
                        catch(Exception ex) 
                        {
                            Console.WriteLine("Error during connection cleanup", ex);
                        }
                    }

                    _connections?.Clear();
                }

                _isRunning = false;
                _listener.Stop();
                _listener.Close();

                Console.WriteLine("WebSocket server stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error stopping WebSocket server", ex);
            }
        }

        private async Task AcceptLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        var wsContext = await context.AcceptWebSocketAsync(null);
                        Console.WriteLine("WebSocket connected.");
                        _ = HandleConnection(wsContext.WebSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Listener was stopped → exit loop gracefully
                    return;
                }
                catch (Exception ex)
                {
                    if (_isRunning) Console.WriteLine("WebSocket error: " + ex.Message);
                }
            }
        }

        private async Task HandleConnection(WebSocket socket)
        {
            var buffer = new byte[1024 * 1024];
            var cts = new CancellationTokenSource();
            
            DisposibleFingerPrintScannerService scanService = null;

            // Track connection
            lock (_lock)
            {
                _connections.Add((socket, cts, scanService));
            }

            try
            {
                //Create once per connection
                scanService = new DisposibleFingerPrintScannerService();

                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        await HandleMessage(socket, buffer, result.Count, scanService, cts.Token);
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        //Signal cancellation to scanner
                        cts.Cancel();
                        scanService?.Cancel();
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        Console.WriteLine("WebSocket client disconnected.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleConnection", ex);
                try
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        var errorResponse = Encoding.UTF8.GetBytes("Error: " + ex.Message);
                        await socket.SendAsync(new ArraySegment<byte>(errorResponse),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                catch(Exception ex1) 
                { 
                    Console.WriteLine("Error sending error message to client", ex1);
                }
            }
            finally
            {
                //Dispose service when socket closes
                scanService?.Dispose();
                socket.Dispose();
                cts.Dispose();
                scanService?.Cancel();
            }
        }

        private async Task HandleMessage(WebSocket socket, byte[] buffer, int count, DisposibleFingerPrintScannerService scanService,
            CancellationToken cancellationToken)
        {
           
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, count);
            Console.WriteLine("Received from WS: " + receivedMessage);

            try
            {
                var receiveModel = JsonConvert.DeserializeObject<SocketEvents<object>>(receivedMessage);

                switch (receiveModel.eventName)
                {
                    case "scanFinger":
                        var scanRequest = JsonConvert.DeserializeObject<BiometricsRequest>(receiveModel.data.ToString());
                        Console.WriteLine("FingerPrint Capture Request: " + receivedMessage);
                        var scanResult = await scanService.FingerPrintScanner(scanRequest, cancellationToken);

                        await SendResponse(socket, "scanFinger", scanResult);


                        //var scanService1 = new FingerPrintScanService();
                        //var scanRequest = JsonConvert.DeserializeObject<BiometricsRequest>(receiveModel.data.ToString());
                        //var scanResult = await scanService1.FingerPrintScanner(scanRequest);

                        //await SendResponse(socket, "scanFinger", scanResult);
                        break;

                    case "fingerVerify":
                        var verifyRequest = JsonConvert.DeserializeObject<FingerPrintVerifyRequest>(receiveModel.data.ToString());
                        Console.WriteLine("FingerPrint Verify Request: " + receivedMessage);
                        if (verifyRequest.IsOneFingerScanner == true)
                        {
                            Console.WriteLine("Using one finger scanner verification method");
                            //VerifyFingerPrintSecuGen  VerifyFingerPrintOneFingerScanner
                            var verifyResult = scanService.VerifyFingerPrintSecuGen(verifyRequest);

                            await SendResponse(socket, "fingerVerify", verifyResult);

                            //break;

                        }
                        else
                        {
                            Console.WriteLine("Using four finger scanner verification method");
                            var verifyResult = await scanService.VerifyFingerPrint(verifyRequest, cancellationToken);

                            await SendResponse(socket, "fingerVerify", verifyResult);

                            //break;
                        }
                        
                        break;

                    case "faceScan":
                        using (var faceService = new FaceCameraService())
                        {
                            var faceScanData = JsonConvert.DeserializeObject<FaceScanRequest>(receiveModel.data.ToString());
                            await faceService.FaceVideoStream(faceScanData, socket, cancellationToken);
                        }

                        //var faceService = new FaceCameraService();
                        //var faceScanData = JsonConvert.DeserializeObject<FaceScanRequest>(receiveModel.data.ToString());
                        //await faceService.FaceVideoStream(faceScanData, socket, CancellationToken.None);
                        break;

                    
                    default:
                        await SendText(socket, "Ok");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while handling message", ex);
                await SendText(socket, "Error: " + ex.Message);
            }
            
        }

        private async Task SendResponse<T>(WebSocket socket, string eventName, T data)
        {
            var response = new SocketEvents<T> { eventName = eventName, data = data };
            var json = JsonConvert.SerializeObject(response);
            await SendText(socket, json);
        }

        private async Task SendText(WebSocket socket, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

      
    }
}
