using BiometricsDemo.Enums;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using Neurotec.Biometrics;
using Neurotec.Biometrics.Client;
using Neurotec.Devices;
using Neurotec.Images;
using Neurotec.Licensing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BiometricsDemo.Client
{
    public class FingerPrintScanService
    {
       
        public async Task<BiometricsResponse> FingerPrintScanner(BiometricsRequest biometricsRequest)
        {
            var Response = new BiometricsResponse();
            var ContentType = "data:image/jpg;base64,";
            var FullImageScan = string.Empty;
            var DirectoryPath = "C:\\BiometricsService";
            string FolderPath = Path.Combine(DirectoryPath, "FingerPrints", $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}");
            try
            {
                var bioType = (Biometric)biometricsRequest.Mode;
                var position = (NFPosition)biometricsRequest.Finger;
                var region = biometricsRequest.RegionCode;
                var penType = (PensionerTypes)biometricsRequest.PensionerType;
                
                if (string.IsNullOrWhiteSpace(biometricsRequest.PensionerCode) || bioType != Biometric.Fingerprint || position < 0)
                {
                    
                    Response.Success = false;
                    Response.Message = "Invalid Details provided -{PensionerCode, BiometricType, Finger}";
                    Response.ErrorMessage = "Invalid Details provided -{PensionerCode, BiometricType, Finger}";
                    return Response;
                }

                if (NLicenseManager.TrialMode == false)
                {
                    //NLicense.Release(license);
                    NLicenseManager.TrialMode = true;// GetTrialModeFlag();
                    Console.WriteLine("Trial mode: " + NLicenseManager.TrialMode);
                }

                const string license = "FingerClient";

                if (!NLicense.Obtain("/local", 5000, license))
                {
                    Response.Success = false;
                    Response.ErrorMessage = $"Could not obtain license: {license}";
                    Response.Message = $"Could not obtain license: {license}";
                    return Response; 
                }


                using (var _biometricClient = new NBiometricClient { UseDeviceManager = true })
                using (var deviceManager = _biometricClient.DeviceManager)
                using (var subject = new NSubject())
                using (var finger = new NFinger())
                {
                   
                  
                    deviceManager.DeviceTypes = NDeviceType.FingerScanner;

                    if (!deviceManager.IsInitialized)
                    {
                        deviceManager.Initialize();

                        if (deviceManager.Devices.Count <= 0)
                        {
                            Response.Success = false;
                            Response.ErrorMessage = $"No fingerprint scanner found.";
                            Response.Message = $"No fingerprint scanner found.";
                            return Response;
                        }
                    }

                    
                    _biometricClient.FingerScanner = (NFScanner)deviceManager.Devices[0];

                    finger.Position = position;

                    subject.Fingers.Add(finger);

                    if (biometricsRequest.MissingFingers != null && biometricsRequest.MissingFingers.Any())
                    {
                        foreach (int missing in biometricsRequest.MissingFingers)
                        {
                            NFPosition missingpos = NFPosition.Unknown;

                            if (position == NFPosition.PlainRightFourFingers)
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
                            else if (position == NFPosition.PlainLeftFourFingers)
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
                            else if (position == NFPosition.PlainThumbs)
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

                    // Capture asynchronously
                    var status = _biometricClient.Capture(subject);


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
                    NBiometricTask task = _biometricClient.CreateTask(NBiometricOperations.Segment
                        | NBiometricOperations.CreateTemplate, subject);

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
                    
                    // Save template
                    //string FolderPath = Path.Combine(DirectoryPath, "FingerPrints", $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}");
                    Directory.CreateDirectory(FolderPath); // Create folder if missing    
                    string FileName = $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}.bat";
                    string FilePath = Path.Combine(FolderPath, FileName);

                    Response.TemplateFilePath = FileName; // FilePath;

                    using (var templateBuffer = subject.GetTemplateBuffer())
                    {

                        File.WriteAllBytes(FilePath, templateBuffer.ToArray());
                    }

                    //If No template file exists, create it
                    //if (!File.Exists(FilePath))
                    //{
                    //    using (var templateBuffer = subject.GetTemplateBuffer())
                    //    {

                    //        File.WriteAllBytes(FilePath, templateBuffer.ToArray());
                    //    }
                    //}
                    //else
                    //{
                    //    //Append scanned finger to the Master template file
                    //    UpdateMasterTemplate(FilePath, subject);
                       
                    //}

                    // Save as JPEG and convert to Base64
                    using (var image = finger.Image)
                    using (var buffer = image.Save(NImageFormat.Jpeg))
                    {
                        byte[] imageBytes = buffer.ToArray();

                        // Save to folder in the project
                        //string folderPath = Path.Combine(DirectoryPath, "FingerPrints", $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}_{biometricsRequest.Finger}");
                        Directory.CreateDirectory(FolderPath); // Create folder if missing

                        string fileName = $"{biometricsRequest.PensionerCode}_{biometricsRequest.PensionerType}_{biometricsRequest.Finger}.jpg";
                        string filePath = Path.Combine(FolderPath, fileName);

                        Response.Path = fileName; // filePath;

                        File.WriteAllBytes(filePath, imageBytes);

                        string base64String = Convert.ToBase64String(imageBytes);
                        FullImageScan = $"{ContentType}{base64String}";
                        //return Response;
                    }

                    var FingerData = subject.Fingers.ToList();

                    // Extract Fingers From the template
                    var FingerDetails = new List<FingerData>();

                    //Check If only one finger
                    if(FingerData.Count <= 1)
                    {
                        var FingerFromTemplate = FingerData[0];

                        if (FingerFromTemplate.Image != null)
                        {
                            using (var image = FingerFromTemplate.Image)
                            using (var buffer = image.Save(NImageFormat.Jpeg))
                            {
                                byte[] imageBytes = buffer.ToArray();

                                string base64String = Convert.ToBase64String(imageBytes);
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

                    if(FingerData.Count > 1)
                    {
                        for(int i = 1; i < FingerData.Count; i++)
                        {
                            var FingerFromTemplate = FingerData[i];

                            if (FingerFromTemplate.Image != null)
                            {
                                using (var image = FingerFromTemplate.Image)
                                using (var buffer = image.Save(NImageFormat.Jpeg))
                                {
                                    byte[] imageBytes = buffer.ToArray();

                                    string base64String = Convert.ToBase64String(imageBytes);
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
                    Response.TemplateBase64 = Convert.ToBase64String(File.ReadAllBytes(FilePath));
                    Response.BiometricData = FingerDetails;
                    Response.Message = "Fingerprint captured successfully";
                    

                    return Response;
                }

            }
            catch (Exception ex)
            {
                // Handle exceptions
                Response.Success = false;
                Response.ErrorMessage = ex.Message;
                Response.Message = ex.Message;
                Console.WriteLine($"Error: {ex.Message}");
                return Response; // or appropriate error response
            }
        }

        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!_disposedValue)
        //    {
        //        if (disposing)
        //        {
        //            // TODO: dispose managed state (managed objects)
        //        }

        //        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        //        // TODO: set large fields to null
        //        _disposedValue = true;
        //    }
        //}

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FingerPrintScanService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        //void IDisposable.Dispose()
        //{
        //    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //    Dispose(disposing: true);
        //    GC.SuppressFinalize(this);
        //}
    }
}
