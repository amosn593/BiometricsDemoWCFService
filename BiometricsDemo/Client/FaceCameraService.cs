using BiometricsDemo.Helpers;
using BiometricsDemo.Models;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using DirectShowLib;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Neurotec.Biometrics;
using Neurotec.Biometrics.Client;
using Neurotec.Licensing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace BiometricsDemo.Client
{
    public class FaceCameraService : IDisposable
    {
        private volatile byte[] _lastFrame;
        private volatile bool _stopStreaming = false;
        private WebSocket _currentSocket;
        private bool _disposed = false;

        // Implement IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Free managed resources
                if (_currentSocket != null)
                {
                    if (_currentSocket.State == WebSocketState.Open)
                    {
                        _currentSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Service disposed",
                            CancellationToken.None
                        ).GetAwaiter().GetResult();
                    }
                    _currentSocket.Dispose();
                    _currentSocket = null;
                }

                
            }

          

            _disposed = true;
        }

        public FaceCameraService()
        {
            Dispose(false);
        }

        public async Task<SocketEvents<string>> FaceVideoStream(FaceScanRequest faceScanRequest, WebSocket socket, CancellationToken cancellationToken)
        {
            try
            {
                _currentSocket = socket;
                _stopStreaming = false;
                
                var cameraIndex = GetSelectedCameraIndex(faceScanRequest.CameraName);

                Console.WriteLine($"Using camera index: {cameraIndex}");

                using (var capture = new VideoCapture(cameraIndex, VideoCapture.API.DShow))
                using (var mat = new Mat())
                {

                    
                    //capture.SetCaptureProperty(CapProp.FrameWidth, 1280);
                    //capture.SetCaptureProperty(CapProp.FrameHeight, 720);
                    capture.SetCaptureProperty(CapProp.Fps, 30);

                    //Warm - up frames
                    for (int i = 0; i < 3; i++)
                        capture.Read(mat);

                    while (capture.IsOpened && socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                    {
                        if (socket.State != WebSocketState.Open)
                        {
                            Console.WriteLine("WebSocket closed, stopping camera stream.");
                            break;
                        }

                        capture.Read(mat);
                        if (!mat.IsEmpty)
                        {
                            try
                            {
                                // Encode frame → JPEG → Base64
                                _lastFrame = mat.ToImage<Emgu.CV.Structure.Bgr, byte>().ToJpegData();

                                //_lastFrame = buffer;

                                // Create output folder if it doesn't exist
                                var DirectoryPath = "C:\\BiometricsService";
                                string outputDir = Path.Combine(DirectoryPath, "FaceFrames", $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}");
                                Directory.CreateDirectory(outputDir);


                                // Save with timestamp
                                string filePath = Path.Combine(outputDir, $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.jpg");
                                File.WriteAllBytes(filePath, _lastFrame);

                                var Base64String = Convert.ToBase64String(_lastFrame);

                                var Base64Image = $"data:image/jpg;base64,{Base64String}";

                                var Response = new FaceStreamResponseModel
                                {
                                    Success = true,
                                    Message = "Frame captured successfully.",
                                    Base64Image = Base64Image
                                };

                                var socketEventResults = new SocketEvents<string>
                                {
                                    eventName = "faceScan",
                                    data = Base64Image //Response
                                };

                                var json = JsonConvert.SerializeObject(socketEventResults);
                                var bytes = Encoding.UTF8.GetBytes(json);

                                await socket.SendAsync(
                                    new ArraySegment<byte>(bytes),
                                    WebSocketMessageType.Text,
                                    true,
                                    cancellationToken
                                );
                            }
                            catch (WebSocketException)
                            {
                                Console.WriteLine("WebSocket exception, stopping stream.");
                                break;
                            }
                            catch (ObjectDisposedException)
                            {
                                Console.WriteLine("WebSocket disposed, stopping camera stream.");
                                break;
                            }

                        }

                        await Task.Delay(33, cancellationToken); // ~10 FPS
                    }
                }

                Console.WriteLine("WebSocket disposed, stopped camera stream.");

                var ResponseError = new FaceStreamResponseModel
                {
                    Success = false,
                    Message = "Error Occurred",
                    Base64Image = string.Empty
                };

                var socketEventResultsError = new SocketEvents<string>
                {
                    eventName = "faceScan",
                    data = "Error Occurred" // ResponseError
                };

                return  socketEventResultsError; // nothing more to return, we stream instead
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in FaceVideoStream: " + ex.Message);

                var ResponseError = new FaceStreamResponseModel
                {
                    Success = false,
                    Message = ex.Message,
                    Base64Image = string.Empty
                };

                var socketEventResultsError = new SocketEvents<string>
                {
                    eventName = "faceScan",
                    data = ex.Message //ResponseError
                };

                var json = JsonConvert.SerializeObject(socketEventResultsError);
                var bytes = Encoding.UTF8.GetBytes(json);

                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        cancellationToken);
                }

                return socketEventResultsError;
            }
        }

        public async Task ReceiveLoop(WebSocket socket, FaceCameraService service, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];

            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var ReceiveModelData = JsonConvert.DeserializeObject<SocketEvents<object>>(json);

                if (ReceiveModelData?.eventName == "faceCapture")
                {
                    var FaceCaptureData = JsonConvert.DeserializeObject<FaceScanRequest>(ReceiveModelData.data.ToString());

                    var filePath =  await service.SaveLastFrame(FaceCaptureData);

                    var socketEventResults = new SocketEvents<FaceScanResponse>
                    {
                        eventName = "saveFrame",
                        data = filePath
                    };

                    var responseJson = JsonConvert.SerializeObject(socketEventResults);
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                    await socket.SendAsync(new ArraySegment<byte>(responseBytes),
                        WebSocketMessageType.Text,
                        true,
                        cancellationToken);
                }
            }
        }

        public async Task<FaceScanResponse> SaveLastFrame(FaceScanRequest faceScanRequest)
        {
            var Response = new FaceScanResponse();
            try
            {
               
                var DirectoryPath = "C:\\BiometricsService";
                string outputDir = Path.Combine(DirectoryPath, "FaceFrames", $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}");
                Directory.CreateDirectory(outputDir);

                // Save with timestamp
                string filePath = Path.Combine(outputDir, $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.jpg");
                string TemplatePath = Path.Combine(outputDir, $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.bat");

                // Validate the last frame has a single high-quality face
                var isValidFace = DetectSingleHighQualityFace(filePath);

                if (isValidFace == false)
                {
                    Response.Success = false;
                    Response.Message = "No face or multiple faces detected";
                    return Response;
                }

                //Read Image from disk into Base64
                byte[] fileBytes = File.ReadAllBytes(filePath);
                string base64 = Convert.ToBase64String(fileBytes);


               
                Response.TemplateBase64 = string.Empty;
                Response.TemplatePath = $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.bat";

                Response.fileName = $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.jpg";
                Response.imagepath = $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.jpg";

                var Base64Image = $"data:image/jpg;base64,{base64}";
                Response.Success = true;
                Response.Message = "Face saved successfully.";
                Response.base64image = Base64Image;
                return Response;
            }
            catch (Exception ex)
            {
                Response.Success = false;
                Response.Message = "Error: " + ex.Message;
                return Response;
            }
            
        }

        public async Task<FaceScanResponse> GetFaceFrame(FaceScanRequest faceScanRequest)
        {
            var Response = new FaceScanResponse();
            try
            {

                var DirectoryPath = "C:\\BiometricsService";
                string outputDir = Path.Combine(DirectoryPath, "FaceFrames", $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}");
                Directory.CreateDirectory(outputDir);

                // Save with timestamp
                string filePath = Path.Combine(outputDir, $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.jpg");
                string TemplatePath = Path.Combine(outputDir, $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.bat");

                const string license = "FaceClient";
                //const string license = "FaceClient";
                //const string license = "FaceFastExtractor";
                //const string license = "SentiVeillance";

                if (NLicenseManager.TrialMode == false)
                {
                    //NLicense.Release(license);
                    NLicenseManager.TrialMode = true;// GetTrialModeFlag();
                    Console.WriteLine("Trial mode: " + NLicenseManager.TrialMode);
                }


                if (!NLicense.Obtain("/local", 5000, license))
                {
                    Response.Success = false;
                    Response.Message = $"Could not obtain license: {license}";
                    Response.ErrorMessage = $"Could not obtain license: {license}";
                    return Response;
                }

               
                using (var biometricClient = new NBiometricClient())
                using (var subject = new NSubject())
                using (var face = new NFace())
                {

                    //Read face image from file and add it to NFace object
                    face.FileName = filePath;
                    //Define that the face source will be a stream
                    face.CaptureOptions = NBiometricCaptureOptions.None;

                    //Read face image from file and add it to NSubject
                    subject.Faces.Add(face);

                    //Detect all faces features
                    bool isAdditionalFunctionalityEnabled = license.Equals("FaceClient") || license.Equals("FaceExtractor") || license.Equals("SentiVeillance");
                    biometricClient.FacesDetectAllFeaturePoints = isAdditionalFunctionalityEnabled;

                    biometricClient.FacesDetectAllFeaturePoints = true;
                    biometricClient.FacesDetermineGender = true;
                    biometricClient.FacesDetermineAge = true;
                    biometricClient.FacesDetectProperties = true;

                    // Create template from added face image
                    var status = biometricClient.CreateTemplate(subject);


                    if (status == NBiometricStatus.Ok)
                    {
                        Console.WriteLine("Template extracted");
                        // Save compressed template to file
                        var Bytes = subject.GetTemplateBuffer().ToArray();
                        File.WriteAllBytes(TemplatePath, Bytes);
                        Console.WriteLine("Template saved successfully");
                        //return Convert.ToBase64String(Bytes);
                        // Read back from disk into Base64
                        byte[] ImageBytes = File.ReadAllBytes(filePath);
                        string Imagebase64 = Convert.ToBase64String(ImageBytes);

                        Response.TemplateBase64 = Convert.ToBase64String(Bytes);
                        Response.TemplatePath = $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.bat";

                        Response.fileName = $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.jpg";
                        Response.imagepath = $"{faceScanRequest.PensionerCode}_{faceScanRequest.PensionerType}.jpg";

                        var NewBase64Image = $"data:image/jpg;base64,{Imagebase64}";
                        Response.Success = true;
                        Response.Message = "Face saved successfully.";
                        Response.base64image = NewBase64Image;
                        return Response;
                    }
                    else
                    {
                        Response.Success = false;
                        Response.Message = $"Error in template creation: {status}";
                        Response.ErrorMessage = $"Error in template creation: {status}";
                        return Response;
                    }
                }

            }
            catch (Exception ex)
            {
                Response.Success = false;
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                Response.ErrorMessage = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                return Response;
            }

        }

        private bool DetectSingleHighQualityFace(string imagePath)
        {
            try
            {
                // Dynamically locate the cascade file in the output directory
                var _haarCascadePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "haarcascade_frontalface_default.xml");

                if (!File.Exists(_haarCascadePath))
                {
                    throw new FileNotFoundException($"Haar Cascade file not found at: {_haarCascadePath}");
                }

                using (var image = new Image<Bgr, byte>(imagePath))
                using (var gray = image.Convert<Gray, byte>())
                {
                    var faceCascade = new CascadeClassifier(_haarCascadePath);

                    // Detect faces
                    var faces = faceCascade.DetectMultiScale(
                        gray,
                        scaleFactor: 1.05,
                        minNeighbors: 8,
                        minSize: new Size(100, 100)
                    );

                    // Ensure exactly one face is detected
                    if (faces.Length == 1)
                    {
                        return true;
                        //Rectangle faceRect = faces[0];

                        //// Extract the face region
                        //var faceRegion = new Image<Bgr, byte>(image.Bitmap).GetSubRect(faceRect);

                        //// Optional: resize the face to a standard high-quality size
                        //var resizedFace = faceRegion.Resize(256, 256, Emgu.CV.CvEnum.Inter.Linear);

                        //// Return cropped face as a Bitmap
                        //return resizedFace.ToBitmap();
                    }

                    // No face or multiple faces detected
                    Console.WriteLine($"No face or multiple faces detected; Faces detected: {faces.Length}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in DetectSingleHighQualityFace: " + ex.Message);
                return false;
            }
        }

        private static int GetSelectedCameraIndex(string cameraName)
        {
            int cameraIndex = 0;
            try
            {
                
                if (!string.IsNullOrWhiteSpace(cameraName))
                {
                    int? SelectedCamera = GetCameraIndexByName(cameraName.Trim()); //Logi
                    if (SelectedCamera is null)
                    {
                        cameraIndex = 0; // default to first camera
                    }
                    else
                    {
                        cameraIndex = SelectedCamera.Value;
                    }
                }
                else
                {
                    int? logiIndex = GetCameraIndexByName("Logi"); //Logi
                    if (logiIndex is null)
                    {
                        cameraIndex = 0; // default to first camera
                    }
                    else
                    {
                        cameraIndex = logiIndex.Value;
                    }


                }

                return cameraIndex;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetSelectedCameraIndex: " + ex.Message);
                return cameraIndex;
            }
        }

        private static int? GetCameraIndexByName(string keyword)
        {
            //var videoDevices = new FilterInfoCollection(AForge.Video.DirectShow.FilterCategory.VideoInputDevice);
            var devices = DsDevice.GetDevicesOfCat(DirectShowLib.FilterCategory.VideoInputDevice);
            for (int i = 0; i < devices.Length; i++)
            {
                Console.WriteLine($"{i}: {devices[i].Name}");
                if (devices[i].Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return i; // return index for Emgu.CV
                }
            }

            return null; // not found
        }

        public List<CameraListResponseModel> GetConnectedCameras()
        {
            var devices = DsDevice.GetDevicesOfCat(DirectShowLib.FilterCategory.VideoInputDevice);

            //var videoDevices = new FilterInfoCollection(AForge.Video.DirectShow.FilterCategory.VideoInputDevice);
            var cameraList = new List<CameraListResponseModel>();
            for (int i = 0; i < devices.Length; i++)
            {
                cameraList.Add(new CameraListResponseModel
                {
                    Index = i,
                    Name = devices[i].Name //.Name
                });
                Console.WriteLine($"{i}: {devices[i].Name}");
               
            }

            return cameraList; // not found
        }
    
    }
}
