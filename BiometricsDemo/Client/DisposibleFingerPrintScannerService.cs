using BiometricsDemo.Enums;
using BiometricsDemo.Helpers;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using Neurotec.Biometrics;
using Neurotec.Biometrics.Client;
using Neurotec.Devices;
using Neurotec.Images;
using Neurotec.IO;
using Neurotec.Licensing;
using NITGEN.SDK.NBioBSP;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SecuGen.FDxSDKPro.Windows;

namespace BiometricsDemo.Client
{
    public class DisposibleFingerPrintScannerService : IDisposable
    {
        private NBiometricClient _biometricClient;
        private NDeviceManager _deviceManager;
        private bool _disposed;
        public DisposibleFingerPrintScannerService()
        {
            _biometricClient = new NBiometricClient { UseDeviceManager = true };
            _deviceManager = _biometricClient.DeviceManager;
        }

        public async Task<BiometricsResponse> FingerPrintScanner(BiometricsRequest biometricsRequest,
            CancellationToken cancellationToken)
        {
            var Response = new BiometricsResponse();
            var ContentType = "data:image/jpg;base64,";
            var FullImageScan = string.Empty;
            var DirectoryPath = "C:\\BiometricsService";
            string FolderPath = Path.Combine(DirectoryPath, "FingerPrints", $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}");

            try
            {
                
                int[] targets = { 13, 14, 15 }; // Allowed Fingers - Right Four Fingers, Left Four Fingers, Both Thumbs
                
                if (!targets.Contains(biometricsRequest.Finger))
                {
                    Response.Success = false;
                    Response.Message = "Wrong Hand Fingers Selected";
                    Response.ErrorMessage = "Wrong Hand Fingers Selected";
                    return Response;
                }

                if (string.IsNullOrWhiteSpace(biometricsRequest.PensionerCode) || biometricsRequest.PensionerType <= 0
                    || (Biometric)biometricsRequest.Mode != Biometric.Fingerprint)
                {

                    Response.Success = false;
                    Response.Message = "Invalid Details provided -{PensionerCode, BiometricType, Finger}";
                    Response.ErrorMessage = "Invalid Details provided -{PensionerCode, BiometricType, Finger}";
                    return Response;
                }

                if (_disposed)
                    throw new ObjectDisposedException(nameof(DisposibleFingerPrintScannerService));

                // check for cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();

                if (_biometricClient == null || _deviceManager == null)
                    throw new InvalidOperationException("Biometric client not initialized");

                

                const string license = "FingerClient";

                if (NLicenseManager.TrialMode == false)
                {
                    //NLicense.Release(license);
                    NLicenseManager.TrialMode = true;// GetTrialModeFlag();
                    Console.WriteLine("Trial mode: " + NLicenseManager.TrialMode);
                }

                if (!NLicense.Obtain("/local", 5000, license))
                {
                    Response.Success = false;
                    Response.ErrorMessage = $"Could not obtain license: {license}";
                    Response.Message = $"Could not obtain license: {license}";
                    return Response;
                }

               
                //Initialize device manager if needed
                _deviceManager.DeviceTypes = NDeviceType.FingerScanner;
                if (!_deviceManager.IsInitialized)
                {
                    _deviceManager.Initialize();
                }

                if (_deviceManager.Devices.Count <= 0)
                {
                    Response.Success = false;
                    Response.Message = "No fingerprint scanner found.";
                    Response.ErrorMessage = "No fingerprint scanner found.";
                    return Response;
                }

                _biometricClient.FingerScanner = (NFScanner)_deviceManager.Devices[0];

                using (var subject = new NSubject())
                using (var finger = new NFinger { Position = (NFPosition)biometricsRequest.Finger })
                {
                    subject.Fingers.Add(finger);

                    if (biometricsRequest.MissingFingers != null && biometricsRequest.MissingFingers.Any())
                    {
                        foreach (int missing in biometricsRequest.MissingFingers)
                        {
                            NFPosition missingpos = NFPosition.Unknown;

                            if ((NFPosition)biometricsRequest.Finger == NFPosition.PlainRightFourFingers)
                            {
                                switch (missing)
                                {
                                    case 1:
                                        missingpos = NFPosition.RightLittleFinger;
                                        break;
                                    case 2:
                                        missingpos = NFPosition.RightRingFinger;
                                        break;
                                    case 3:
                                        missingpos = NFPosition.RightMiddle;
                                        break;
                                    case 4:
                                        missingpos = NFPosition.RightIndexFinger;
                                        break;

                                }

                            }
                            else if ((NFPosition)biometricsRequest.Finger == NFPosition.PlainLeftFourFingers)
                            {
                                switch (missing)
                                {
                                    case 1:
                                        missingpos = NFPosition.LeftLittleFinger;
                                        break;
                                    case 2:
                                        missingpos = NFPosition.LeftRingFinger;
                                        break;
                                    case 3:
                                        missingpos = NFPosition.LeftMiddle;
                                        break;
                                    case 4:
                                        missingpos = NFPosition.LeftIndexFinger;
                                        break;

                                }
                            }
                            else if ((NFPosition)biometricsRequest.Finger == NFPosition.PlainThumbs)
                            {
                                switch (missing)
                                {
                                    case 1:
                                        missingpos = NFPosition.LeftThumb;
                                        break;
                                    case 2:
                                        missingpos = NFPosition.RightThumb;
                                        break;

                                }
                            }
                            subject.MissingFingers.Add(missingpos);
                        }
                    }

                   

                    //Build capture + template creation task
                    var taskcapture = _biometricClient.CreateTask(
                        NBiometricOperations.Capture, subject);

                    // Run on background so we can await + cancel
                    var performTask = Task.Run(() =>
                    {
                        _biometricClient.PerformTask(taskcapture);
                        return taskcapture.Status;
                    }, cancellationToken);

                    // if websocket closes → cancel biometric client
                    using (cancellationToken.Register(() => _biometricClient.Cancel()))
                    {
                        
                        var status = await performTask;

                        cancellationToken.ThrowIfCancellationRequested();

                        if (status != NBiometricStatus.Ok)
                        {
                            Response.Success = false;
                            Response.ErrorMessage = $"Fingerprint capture failed: {status}";
                            Response.Message = $"Fingerprint capture failed: {status}";
                            return Response;
                        }

                        _biometricClient.FingersTemplateSize = NTemplateSize.Large;

                        //status = _biometricClient.CreateTemplate(subject);

                        // Create Segment and Create Template task
                        NBiometricTask task = _biometricClient.CreateTask(NBiometricOperations.Segment |
                            NBiometricOperations.CreateTemplate, subject);

                        // Perform task
                        _biometricClient.PerformTask(task);

                        // Get task status
                        status = task.Status;

                        if (status != NBiometricStatus.Ok)
                        {
                            Response.Success = false;
                            Response.ErrorMessage = $"Template creation failed: {status}";
                            Response.Message = $"Template creation failed: {status}";
                            return Response;
                        }

                        if (status == NBiometricStatus.Ok && (finger.WrongHandWarning))
                        {
                            Response.Success = false;
                            Response.WrongHand = true;
                            Response.Message = "You have placed the finger from the wrong hand. Please place the correct hand finger";
                            Response.ErrorMessage = "You have placed the finger from the wrong hand. Please place the correct hand finger";
                            return Response;
                        }

                        Directory.CreateDirectory(FolderPath); // Create folder if missing    
                        string FileName = $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}.bat";
                        string FilePath = Path.Combine(FolderPath, FileName);

                        Response.TemplateFilePath = FileName; // FilePath;

                        using (var templateBuffer = subject.GetTemplateBuffer())
                        {

                            File.WriteAllBytes(FilePath, templateBuffer.ToArray());
                        }


                        // Save as JPEG and convert to Base64
                        using (var image = finger.Image)
                        using (var buffer = image.Save(NImageFormat.Jpeg))
                        {
                            //byte[] imageBytes = buffer.ToArray();

                            // Save to folder in the project
                            //string folderPath = Path.Combine(DirectoryPath, "FingerPrints", $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}_{biometricsRequest.Finger}");
                            Directory.CreateDirectory(FolderPath); // Create folder if missing

                            string fileName = $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}_{biometricsRequest.Finger}.jpg";
                            string filePath = Path.Combine(FolderPath, fileName);

                            Response.Path = fileName; // filePath;

                            File.WriteAllBytes(filePath, buffer.ToArray());

                            string base64String = Convert.ToBase64String(buffer.ToArray());
                            FullImageScan = $"{ContentType}{base64String}";
                            //return Response;
                        }

                        var FingerData = subject.Fingers.ToList();

                        
                        // Extract Fingers From the template
                        var FingerDetails = new List<FingerData>();

                        //Check If only one finger
                        if (FingerData.Count <= 1)
                        {
                            var FingerFromTemplate = FingerData[0];

                            if (FingerFromTemplate.Image != null)
                            {
                                using (var image = FingerFromTemplate.Image)
                                using (var buffer = image.Save(NImageFormat.Jpeg))
                                {
                                    //byte[] imageBytes = buffer.ToArray();

                                    string base64String = Convert.ToBase64String(buffer.ToArray());
                                    FingerDetails.Add(new FingerData
                                    {
                                        filename = $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}_{FingerFromTemplate.Position}.jpg", // FingerFromTemplate.Position.ToString(),
                                        filepath = $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}_{FingerFromTemplate.Position}.jpg", // Path.Combine(FolderPath, $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}_{FingerFromTemplate.Position}.jpg"),
                                        base64 = $"{ContentType}{base64String}",
                                        position = FingerFromTemplate.Position.ToString(),
                                        quality = FingerFromTemplate.Objects[0].Quality.ToString(),
                                        position_int = (int)FingerFromTemplate.Position
                                    });//
                                }
                            }
                        }

                        if (FingerData.Count > 1)
                        {
                            for (int i = 1; i < FingerData.Count; i++)
                            {
                                var FingerFromTemplate = FingerData[i];

                                if (FingerFromTemplate.Image != null)
                                {
                                    using (var image = FingerFromTemplate.Image)
                                    using (var buffer = image.Save(NImageFormat.Jpeg))
                                    {
                                        //byte[] imageBytes = buffer.ToArray();

                                        string base64String = Convert.ToBase64String(buffer.ToArray());
                                        FingerDetails.Add(new FingerData
                                        {
                                            filename = $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}_{FingerFromTemplate.Position}.jpg", // FingerFromTemplate.Position.ToString(),
                                            filepath = $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}_{FingerFromTemplate.Position}.jpg", // Path.Combine(FolderPath, $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}_{FingerFromTemplate.Position}.jpg"),
                                            base64 = $"{ContentType}{base64String}",
                                            position = FingerFromTemplate.Position.ToString(),
                                            quality = FingerFromTemplate.Objects[0].Quality.ToString(),
                                            position_int = (int)FingerFromTemplate.Position
                                        });
                                    }
                                }
                            }
                        }

                        Response.base64 = FullImageScan;
                        Response.TemplateBase64 = Convert.ToBase64String(subject.GetTemplateBuffer().ToArray());
                        Response.BiometricData = FingerDetails;
                        Response.Message = "Fingerprint captured successfully";


                        return Response;
                    }
   
                }


            }
            catch (Exception ex)
            {
                Response.Success = false;
                Response.ErrorMessage = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                return Response;
            }
        }

        public async Task<VerifyServerRequest> VerifyFingerPrint(FingerPrintVerifyRequest request,
            CancellationToken cancellationToken)
        {
            var Response = new VerifyServerRequest();

            try
            {
                
                if (String.IsNullOrWhiteSpace(request.PensionerCode) || (Biometric)request.Mode != Biometric.Fingerprint
                    || request.PensionerType <= 0)
                {
                    Response.Success = false;
                    Response.ErrorMsg = "Invalid PensionerCode or Biometric!!!";
                    Response.Message = "Please provide valid PensionerCode Or PensionerType and select Fingerprint as Biometric.";
                    return Response;
                }

                if (_disposed)
                    throw new ObjectDisposedException(nameof(DisposibleFingerPrintScannerService));

                // check for cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();

                if (_biometricClient == null || _deviceManager == null)
                    throw new InvalidOperationException("Biometric client not initialized");



                const string license = "FingerClient,FingerExtractor";

                if (NLicenseManager.TrialMode == false)
                {
                    //NLicense.Release(license);
                    NLicenseManager.TrialMode = true;// GetTrialModeFlag();
                    Console.WriteLine("Trial mode: " + NLicenseManager.TrialMode);
                }

                if (!NLicense.Obtain("/local", 5000, license))
                {
                    Response.Success = false;
                    Response.ErrorMsg = $"Could not obtain license: {license}";
                    Response.Message = $"Could not obtain license: {license}";
                    return Response;
                }


                //Initialize device manager if needed
                _deviceManager.DeviceTypes = NDeviceType.FingerScanner;
                if (!_deviceManager.IsInitialized)
                {
                    _deviceManager.Initialize();
                }

                if (_deviceManager.Devices.Count <= 0)
                {
                    Response.Success = false;
                    Response.Message = "No fingerprint scanner found.";
                    Response.ErrorMsg = "No fingerprint scanner found.";
                    return Response;
                }

                _biometricClient.FingerScanner = (NFScanner)_deviceManager.Devices[0];

                using (var subject = new NSubject())
                using (var finger = new NFinger { Position = NFPosition.Unknown })
                {
                    subject.Fingers.Add(finger);

                    //Build capture + template creation task
                    var taskcapture = _biometricClient.CreateTask(
                        NBiometricOperations.Capture, subject);

                    // Run on background so we can await + cancel
                    var performTask = Task.Run(() =>
                    {
                        _biometricClient.PerformTask(taskcapture);
                        return taskcapture;
                    }, cancellationToken);

                    // if websocket closes → cancel biometric client
                    using (cancellationToken.Register(() => _biometricClient.Cancel()))
                    {
                        var status = await performTask;

                        cancellationToken.ThrowIfCancellationRequested();

                        if (status.Status != NBiometricStatus.Ok)
                        {
                            Response.Success = false;
                            Response.ErrorMsg = $"Fingerprint capture failed: {status}";
                            Response.Message = $"Fingerprint capture failed: {status}";
                            return Response;
                        }

                        _biometricClient.FingersTemplateSize = NTemplateSize.Large;

                        //status = _biometricClient.CreateTemplate(subject);

                        // Create Segment and Create Template task
                        NBiometricTask task = _biometricClient.CreateTask(
                            NBiometricOperations.CreateTemplate, subject);

                        // Perform task
                        _biometricClient.PerformTask(task);

                        // Get task status
                        var statusCreateTemplate = task.Status;

                        if (statusCreateTemplate != NBiometricStatus.Ok)
                        {
                            Response.Success = false;
                            Response.ErrorMsg = $"Template creation failed: {status}";
                            Response.Message = $"Template creation failed: {status}";
                            return Response;
                        }
                        
                        //string FileName = $"{request.PensionerCode}_{request.PensionerType}.bat";

                        //var TemplateBytes = subject.GetTemplateBuffer().ToArray();

                        //var TemplatesBase64 = Convert.ToBase64String(subject.GetTemplateBuffer().ToArray());

                        //Prepare Verify Server Request
                        var Payload = new VerifyServerRequest
                        {

                            TemplateBase64 = Convert.ToBase64String(subject.GetTemplateBuffer().ToArray()), //$"{Content_Type}{Convert.ToBase64String(TemplatesBase64)}",
                            TemplateFilePath = $"{request.PensionerCode}_{request.PensionerType}.bat",
                            Success = true,
                            ErrorMsg = "Fingerprint captured and template created successfully.",
                            Message = "Fingerprint captured and template created successfully."

                        };

                        return Payload;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during fingerprint verification: " + ex.Message);
                Response.Success = false;
                Response.ErrorMsg = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex); ;
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex); ;
                return Response;
            }
        }

        public VerifyServerRequest VerifyFingerPrintOneFingerScanner(FingerPrintVerifyRequest request)
        {
            //CaptureFinger();

            var Response = new VerifyServerRequest();
            var ContentType = "data:image/jpg;base64,";
            var FullImageScan = string.Empty;
            var DirectoryPath = "C:\\BiometricsService";
            string FolderPath = Path.Combine(DirectoryPath, "Verification_FingerPrints", $"{request.PensionerCode}_{request.PensionerType}");
            Directory.CreateDirectory(FolderPath); // Create folder if missing  
            var RawDataPath = Path.Combine(FolderPath, $"{request.PensionerCode}_{request.PensionerType}.raw");
            var BmpDataPath = Path.Combine(FolderPath, $"{request.PensionerCode}_{request.PensionerType}.bmp");
            var JpgDataPath = Path.Combine(FolderPath, $"{request.PensionerCode}_{request.PensionerType}.jpg");
            try
            {
                if (String.IsNullOrWhiteSpace(request.PensionerCode) || (Biometric)request.Mode != Biometric.Fingerprint
                    || request.PensionerType <= 0)
                {
                    Response.Success = false;
                    Response.ErrorMsg = "Invalid PensionerCode or Biometric!!!";
                    Response.Message = "Please provide valid PensionerCode Or PensionerType and select Fingerprint as Biometric.";
                    return Response;
                }

                NBioAPI m_NBioAPI;
                NBioAPI.Export m_Export;

                short m_OpenedDeviceID;
                NBioAPI.Type.HFIR m_hNewFIR;
                NBioAPI.Type.DEVICE_INFO_EX[] m_DeviceInfoEx;

                m_NBioAPI = new NBioAPI();
                m_Export = new NBioAPI.Export(m_NBioAPI);

                m_OpenedDeviceID = NBioAPI.Type.DEVICE_ID.NONE;
                m_hNewFIR = null;

                NBioAPI.Type.WINDOW_OPTION WinOption;
                NBioAPI.Type.HFIR hAudit;

                WinOption = new NBioAPI.Type.WINDOW_OPTION();
                hAudit = new NBioAPI.Type.HFIR();

                WinOption.WindowStyle = NBioAPI.Type.WINDOW_STYLE.INVISIBLE;

                if (m_hNewFIR != null)
                {
                    // Free native memory
                    m_hNewFIR.Dispose();
                    m_hNewFIR = null;
                }

                // Open device
                short iDeviceID = NBioAPI.Type.DEVICE_ID.AUTO;
                uint ret1 = m_NBioAPI.OpenDevice(iDeviceID);
                if (ret1 != NBioAPI.Error.NONE)
                {
                    Console.WriteLine($"Open Device Failed! - {ret1}");
                    Response.Success = false;
                    Response.ErrorMsg = $"Open Device Failed! - {ret1}";
                    Response.Message = $"Open Device Failed! - {ret1}";
                    return Response;
                }

                uint ret = m_NBioAPI.Capture(NBioAPI.Type.FIR_PURPOSE.AUDIT, out m_hNewFIR, NBioAPI.Type.TIMEOUT.INFINITE, hAudit, WinOption);

                if (ret == NBioAPI.Error.NONE)
                {
                    // If you want to save fir data then this FIR data write to DB or File.
                    NBioAPI.Export.EXPORT_AUDIT_DATA exportData;

                    ret = m_Export.NBioBSPToImage(hAudit, out exportData);

                    if (ret == NBioAPI.Error.NONE)
                    {
                        int nWidth = (int)exportData.ImageWidth;
                        int nHeight = (int)exportData.ImageHeight;

                        {     // raw image save...
                            FileStream fout;

                            fout = new FileStream(RawDataPath, FileMode.Create);
                            fout.Write(exportData.AuditData[0].Image[0].Data, 0, nWidth * nHeight);
                            fout.Close();
                        }

                        {     // bmp image save...
                            Bitmap bmp = new Bitmap(nWidth, nHeight, PixelFormat.Format8bppIndexed);
                            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                            System.Runtime.InteropServices.Marshal.Copy(exportData.AuditData[0].Image[0].Data, 0, data.Scan0, nWidth * nHeight);
                            bmp.UnlockBits(data);

                            ColorPalette GrayscalePalette = bmp.Palette;

                            for (int i = 0; i < 256; i++)
                                GrayscalePalette.Entries[i] = Color.FromArgb(i, i, i);

                            bmp.Palette = GrayscalePalette;

                            bmp.Save(BmpDataPath);
                        }
                    }

                    // Free native memory
                    hAudit.Dispose();

                    //Read the saved bmp file and convert to base64
                    byte[] imageArray = File.ReadAllBytes(BmpDataPath);

                    var Payload = new VerifyServerRequest
                    {
                        ImageBase64 = Convert.ToBase64String(imageArray),
                        TemplateBase64 = String.Empty, //$"{Content_Type}{Convert.ToBase64String(TemplatesBase64)}",
                        TemplateFilePath = $"{request.PensionerCode}_{request.PensionerType}.bat",
                        Success = true,
                        ErrorMsg = "Fingerprint captured and template created successfully.",
                        Message = "Fingerprint captured and template created successfully."

                    };
                    return Payload;
                }
                else
                {
                    Console.WriteLine($"Capture Function Failed! - {ret}");
                    Response.Success = false;
                    Response.ErrorMsg = $"Capture Function Failed! - {ret}";
                    Response.Message = $"Capture Function Failed! - {ret}";
                    return Response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during fingerprint verification: " + ex.Message);
                Response.Success = false;
                Response.ErrorMsg = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex); ;
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex); ;
                return Response;
            }


        }

        public VerifyServerRequest VerifyFingerPrintSecuGen(FingerPrintVerifyRequest request)
        {
            var Response = new VerifyServerRequest();

            var DirectoryPath = "C:\\BiometricsService";
            string FolderPath = Path.Combine(
                DirectoryPath,
                "Verification_FingerPrints",
                $"{request.PensionerCode}_{request.PensionerType}");

            Directory.CreateDirectory(FolderPath);

            try
            {
                if (string.IsNullOrWhiteSpace(request.PensionerCode)
                    || request.PensionerType <= 0
                    || (Biometric)request.Mode != Biometric.Fingerprint)
                {
                    Response.Success = false;
                    Response.ErrorMsg = "Invalid PensionerCode or Biometric";
                    Response.Message = "Please provide valid PensionerCode and Fingerprint mode.";
                    return Response;
                }

                SGFingerPrintManager fpManager = new SGFingerPrintManager();

                SGFPMDeviceName device_name = SGFPMDeviceName.DEV_AUTO;

                //SGFPMDeviceName device_name = SGFPMDeviceName.DEV_UNKNOWN;
                Int32 device_id = (Int32)SGFPMPortAddr.USB_AUTO_DETECT;

                int error;

                // Initialize scanner
                error = fpManager.Init(device_name);

                if (error != (int)SGFPMError.ERROR_NONE)
                {
                    Response.Success = false;
                    Response.ErrorMsg = $"Failed to initialize scanner. Error: {error}";
                    Response.Message = Response.ErrorMsg;
                    return Response;
                }

                // Open scanner device
                error = fpManager.OpenDevice(device_id);

                if (error != (int)SGFPMError.ERROR_NONE)
                {
                    Response.Success = false;
                    Response.ErrorMsg = $"Failed to open scanner device. Error: {error}";
                    Response.Message = Response.ErrorMsg;
                    return Response;
                }

                // Get scanner info
                SGFPMDeviceInfoParam deviceInfo = new SGFPMDeviceInfoParam();

                error = fpManager.GetDeviceInfo(deviceInfo);

                if (error != (int)SGFPMError.ERROR_NONE)
                {
                    Response.Success = false;
                    Response.ErrorMsg = $"Failed to get device information. Error: {error}";
                    Response.Message = Response.ErrorMsg;
                    return Response;
                }

                int imageWidth = deviceInfo.ImageWidth;
                int imageHeight = deviceInfo.ImageHeight;

                byte[] imageBuffer = new byte[imageWidth * imageHeight];

                // Capture fingerprint image
                error = fpManager.GetImage(imageBuffer);

                if (error != (int)SGFPMError.ERROR_NONE)
                {
                    Response.Success = false;
                    Response.ErrorMsg = $"Fingerprint capture failed. Error: {error}";
                    Response.Message = "Place finger properly on the scanner.";
                    return Response;
                }

                
                // Save RAW file
                string rawPath = Path.Combine(
                    FolderPath,
                    $"{request.PensionerCode}_{request.PensionerType}.raw");

                File.WriteAllBytes(rawPath, imageBuffer);

                // Create Bitmap
                Bitmap bmp = new Bitmap(
                    imageWidth,
                    imageHeight,
                    PixelFormat.Format8bppIndexed);

                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format8bppIndexed);

                System.Runtime.InteropServices.Marshal.Copy(
                    imageBuffer,
                    0,
                    bmpData.Scan0,
                    imageWidth * imageHeight);

                bmp.UnlockBits(bmpData);

                // Set grayscale palette
                ColorPalette palette = bmp.Palette;

                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }

                bmp.Palette = palette;

                // Save BMP
                string bmpPath = Path.Combine(
                    FolderPath,
                    $"{request.PensionerCode}_{request.PensionerType}.bmp");

                bmp.Save(bmpPath, ImageFormat.Bmp);

                // Save JPG
                string jpgPath = Path.Combine(
                    FolderPath,
                    $"{request.PensionerCode}_{request.PensionerType}.jpg");

                bmp.Save(jpgPath, ImageFormat.Jpeg);

                // Convert image to Base64
                byte[] imageBytes = File.ReadAllBytes(jpgPath);

               
                // Build response
                Response.Success = true;
                Response.TemplateBase64 = "";
                Response.ImageBase64 = Convert.ToBase64String(imageBytes);
                Response.TemplateFilePath = $"{request.PensionerCode}_{request.PensionerType}.bat";

                Response.ErrorMsg = string.Empty;
                Response.Message = "Fingerprint captured successfully.";

                fpManager.CloseDevice();

                return Response;
            }
            catch (Exception ex)
            {
                Response.Success = false;
                Response.ErrorMsg = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);

                return Response;
            }
        }

        //Force cancel ongoing capture
        public void Cancel()
        {
            if (!_disposed)
            {
                _biometricClient?.Cancel();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (_biometricClient != null)
            {
                _biometricClient.Dispose();
                _biometricClient = null;
            }

            if (_deviceManager != null)
            {
                _deviceManager.Dispose();
                _deviceManager = null;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
