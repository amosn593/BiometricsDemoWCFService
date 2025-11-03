using BiometricsDemo.Enums;
using BiometricsDemo.Helpers;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using BiometricsDemo.Server;
using Neurotec.Biometrics;
using Neurotec.Biometrics.Client;
using Neurotec.Devices;
using Neurotec.Licensing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BiometricsDemo.Client
{
    public class FingerPrintVerifyService
    {
        private NBiometricClient _biometricClient;

        public NBiometricClient BiometricClient
        {
            get { return _biometricClient; }
            set { _biometricClient = value; }
        }

        public async Task<VerifyServerRequest> VerifyFingerPrint(FingerPrintVerifyRequest request)
        {
            var Response = new VerifyServerRequest();
            
            try
            {
                
                
                if (String.IsNullOrWhiteSpace(request.PensionerCode) || request.PensionerType <= 0
                    || (Biometric)request.Mode != Biometric.Fingerprint)
                {
                    Response.Success = false;
                    Response.ErrorMsg = "Invalid PensionerCode or Biometric!!!";
                    Response.Message = "Please provide valid PensionerCode Or PenType and select Fingerprint as Biometric.";
                    return Response;
                }

                const string license = "FingerClient";

                if (NLicenseManager.TrialMode == false)
                {
                    NLicense.Release(license);
                    //NLicenseManager.TrialMode = true;// GetTrialModeFlag();
                    Console.WriteLine("Trial mode: " + NLicenseManager.TrialMode);
                }

                 //,FingerMatcher

                if (!NLicense.Obtain("/local", 5000, license))
                {
                    Response.Success = false;
                    Response.ErrorMsg = $"Could not obtain license: {license}";
                    Response.Message = $"Could not obtain license: {license}";
                    return Response;
                }

                
                using (var biometricClient = new NBiometricClient { UseDeviceManager = true })
                using (var deviceManager = biometricClient.DeviceManager)
                using (var subject = new NSubject())
                using (var finger = new NFinger())
                {
                    _biometricClient = biometricClient;

                    // Set type of the device used
                    deviceManager.DeviceTypes = NDeviceType.FingerScanner;

                    // Initialize the NDeviceManager
                    if (!deviceManager.IsInitialized)
                    {
                        deviceManager.Initialize();

                        if (deviceManager.Devices.Count <= 0)
                        {
                            Response.Success = false;
                            Response.ErrorMsg = $"No fingerprint scanner found.";
                            Response.Message = $"No fingerprint scanner found.";
                            return Response;
                        }
                    }


                    // Set the selected finger scanner as NBiometricClient Finger Scanner
                    _biometricClient.FingerScanner = (NFScanner)deviceManager.Devices[0];


                    // Add NFinger to NSubject
                    finger.Position = NFPosition.Unknown;

                    subject.Fingers.Add(finger);

                    // Capture asynchronously
                    var status = _biometricClient.Capture(subject);


                    if (status != NBiometricStatus.Ok)
                    {
                        Response.Success = false;
                        Response.ErrorMsg = $"Fingerprint capture failed: {status}";
                        Response.Message = $"Fingerprint capture failed: {status}";
                        return Response;
                    }

                    _biometricClient.FingersTemplateSize = NTemplateSize.Large;

                    status = _biometricClient.CreateTemplate(subject);
                    if (status != NBiometricStatus.Ok)
                    {
                        Response.Success = false;
                        Response.ErrorMsg = $"Template creation failed: {status}";
                        Response.Message = $"Template creation failed: {status}";
                        return Response;
                    }


                    
                    string FileName = $"{request.PensionerCode}_{request.PensionerType}.bat";
                    
                    var TemplateBytes = subject.GetTemplateBuffer().ToArray();

                    var TemplatesBase64 = Convert.ToBase64String(TemplateBytes);

                    //Prepare Verify Server Request
                    var Payload = new VerifyServerRequest
                    {
                        
                        TemplateBase64 = TemplatesBase64, //$"{Content_Type}{Convert.ToBase64String(TemplatesBase64)}",
                        TemplateFilePath = FileName,
                        Success = true,
                        ErrorMsg = "Fingerprint captured and template created successfully.",
                        Message = "Fingerprint captured and template created successfully."
                  
                    };

                    return Payload;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during fingerprint verification: " + ex.Message);
                Response.Success = false;
                Response.ErrorMsg = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                return Response;
            }
        }


    }
}
