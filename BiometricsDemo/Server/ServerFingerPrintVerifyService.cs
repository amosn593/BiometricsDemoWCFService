using BiometricsDemo.Enums;
using BiometricsDemo.Helpers;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using Neurotec.Biometrics;
using Neurotec.Biometrics.Client;
using Neurotec.Images;
using Neurotec.IO;
using Neurotec.Licensing;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.Server
{
    public class ServerFingerPrintVerifyService
    {
        private NBiometricClient _biometricClient;

        public NBiometricClient BiometricClient
        {
            get { return _biometricClient; }
            set { _biometricClient = value; }
        }

        public async Task<ServerRequestResponse> VerifyFingerPrint(VerifyServerRequest verifyServerRequest)
        {
            var Response = new ServerRequestResponse();
            try
            {
                
                var DirectoryPath = "C:\\BiometricsServer";
                string FolderPath = Path.Combine(DirectoryPath, "FingerPrints", $"{verifyServerRequest.pensionerCode}_{verifyServerRequest.pensionerType}");

                if (string.IsNullOrEmpty(verifyServerRequest.TemplateBase64) || verifyServerRequest.pensionerCode <= 0 || verifyServerRequest.pensionerType <= 0)
                {
                    Response.Success = false;
                    Response.ErrorMsg = "Invalid PensionerCode or PensionerType or Biometric!!!";
                    Response.Status = "UnVerified";
                    Response.Match = false;
                    return Response;
                }

                const string license = "FingerClient,FingerMatcher";

                if (NLicenseManager.TrialMode == false)
                {
                    NLicense.Release(license);
                    //NLicenseManager.TrialMode = true;// GetTrialModeFlag();
                    Console.WriteLine("Trial mode: " + NLicenseManager.TrialMode);
                }

                

                if (!NLicense.Obtain("/local", 5000, license))
                {
                    Response.Success = false;
                    Response.ErrorMsg = $"Could not obtain license: {license}";
                    Response.Message = "Could not obtain license.";
                    Response.Status = "UnVerified";
                    Response.Match = false;
                    return Response;
                }

                using (var biometricClient = new NBiometricClient { })
                {
                    _biometricClient = biometricClient;

                    //Create a subject from saved template
                    var TemplateName = Path.Combine(FolderPath, $"{verifyServerRequest.TemplateFilePath}");

                    if (!File.Exists(TemplateName))
                    {
                        Response.Success = false;
                        Response.ErrorMsg = $"Could not obtain fingerprint biometrics";
                        Response.Status = "UnVerified";
                        Response.Message = "Template file does not exist.";
                        Response.Match = false;
                        return Response;
                    }

                    var MasterBytes = File.ReadAllBytes(TemplateName);

                    var OriginalTemplateBase64 = Convert.ToBase64String(MasterBytes);

                    NSubject SubjectFromSavedTemplate = SubjectFromTemplateBase64(OriginalTemplateBase64);


                    // Create a temmplate from the base64string
                    NSubject SubjectFromBase64 = null;

                    //Check if Template or Imagebase64 submitted
                    if(string.IsNullOrWhiteSpace(verifyServerRequest.TemplateBase64) && !string.IsNullOrWhiteSpace(verifyServerRequest.ImageBase64))
                    {
                        // Create template from image base64
                        
                        SubjectFromBase64 = CreateSubjectFromImageBase64(verifyServerRequest.ImageBase64, "imagebase64");
                        
                    }
                    else
                    {
                        SubjectFromBase64 = SubjectFromTemplateBase64(verifyServerRequest.TemplateBase64);
                    }

                    
                    if (SubjectFromBase64 is null || SubjectFromSavedTemplate is null)
                    {
                        Response.Success = false;
                        Response.ErrorMsg = "Null reference or candidate subject template.";
                        Response.Status = "UnVerified";
                        Response.Message = "Null reference or candidate subject template.";
                        Response.Match = false;
                        return Response;
                    }

                    //match the two templates
                    //Return the result
                    _biometricClient.MatchingThreshold = 96;

                    // Set matching speed
                    _biometricClient.FingersMatchingSpeed = NMatchingSpeed.Medium;

                    var Verifystatus = _biometricClient.Verify(SubjectFromSavedTemplate, SubjectFromBase64);

                    if (Verifystatus == NBiometricStatus.Ok)
                    {
                        Response.Success = true;
                        Response.ErrorMsg = $"Verification success";
                        Response.Status = "Verified";
                        Response.Match = true;
                        return Response;
                    }
                    Response.Success = false;
                    Response.ErrorMsg = $"Verification unsuccess";
                    Response.Message = "Fingerprints did not match.";
                    Response.Status = "UnVerified";
                    Response.Match = false;
                    return Response;
                }
            }
            catch (Exception ex)
            {
                Response.Success = false;
                Response.ErrorMsg = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                Response.Status = "UnVerified";
                Response.Match = false;
                return Response;
            }
            
        }

        private static NSubject CreateSubjectFromTemplate(string templateName)
        {
            NSubject subject = null;
            subject = NSubject.FromFile(templateName);

            return subject;

        }

        private static NSubject SubjectFromTemplateBase64(string b64)
        {
            if (string.IsNullOrWhiteSpace(b64)) throw new ArgumentException("Empty base64.");

            byte[] bytes = Convert.FromBase64String(b64);

            // Option A (newer SDKs): set template buffer directly
            var subject = new NSubject();
            subject.SetTemplateBuffer(new NBuffer(bytes));
            return subject;

        }

        private NSubject CreateSubjectFromImageBase64(string base64Image, string id)
        {
            // Step 1: Decode Base64 string to byte array
          
            byte[] imageBytes = Convert.FromBase64String(base64Image);

            // Step 2: Load image from byte array
            
            using (var ms = new MemoryStream(imageBytes))
            using (var nstream = NStream.FromStream(ms)) //wrap MemoryStream
            using (var image = NImage.FromStream(nstream))
            {
                NFinger finger = new NFinger();
                finger.Image = image;

                NSubject subject = new NSubject();
                subject.Fingers.Add(finger);

                return subject;
            }

            
        }

    }
}
