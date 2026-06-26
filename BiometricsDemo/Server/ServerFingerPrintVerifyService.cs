using BiometricsDemo.Helpers;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using Neurotec.Biometrics;
using Neurotec.Biometrics.Client;
using Neurotec.Images;
using Neurotec.IO;
using Neurotec.Licensing;
using System;
using System.IO;
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
                
                var DirectoryPath = "C:\\BiometricsServerUAT";
                string FolderPath = Path.Combine(DirectoryPath, "FingerPrints", $"{verifyServerRequest.pensionerCode}_{verifyServerRequest.pensionerType}");

                Console.WriteLine($"Verify FingerPrint, Folder path: {FolderPath}");

                if (verifyServerRequest.pensionerCode <= 0 || verifyServerRequest.pensionerType <= 0)
                {
                    Response.Success = false;
                    Response.ErrorMsg = "Invalid PensionerCode or PensionerType or Biometric!!!";
                    Response.Message = "Invalid PensionerCode or PensionerType or Biometric!!!";
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
                    Response.Message = "Could not obtain license: {license}";
                    Response.Status = "UnVerified";
                    Response.Match = false;
                    return Response;
                }

                // Ensure at least one biometric exists
                bool hasTemplate =
                    !string.IsNullOrWhiteSpace(verifyServerRequest.TemplateBase64);

                bool hasImage =
                    !string.IsNullOrWhiteSpace(verifyServerRequest.ImageBase64);

                if (!hasTemplate && !hasImage)
                {
                    Response.Success = false;
                    Response.ErrorMsg = "Fingerprint template or image is required.";
                    Response.Message = "Fingerprint template or image is required.";
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

                    // Candidate subject
                    NSubject SubjectFromCandidate;

                    // ============================================
                    // OPTION 1: USE EXISTING TEMPLATE
                    // ============================================
                    if (hasTemplate)
                    {
                        SubjectFromCandidate =
                            SubjectFromTemplateBase64(
                                verifyServerRequest.TemplateBase64);
                    }
                    // ============================================
                    // OPTION 2: CREATE TEMPLATE FROM IMAGE
                    // ============================================
                    else
                    {
                        byte[] imageBytes = Convert.FromBase64String(verifyServerRequest.ImageBase64);

                        //var subject1 = CreateFingerSubjectFromImage(imageBytes, "subject1");


                        //SubjectFromCandidate =
                        //    CreateSubjectFromImageBase64(
                        //        verifyServerRequest.ImageBase64, verifyServerRequest.pensionerCode.ToString(),
                        //        biometricClient);

                        SubjectFromCandidate = CreateFingerSubjectFromImage(imageBytes, "subject1");



                    }

                    // Create a temmplate from the base64string
                    //NSubject SubjectFromBase64 = SubjectFromTemplateBase64(verifyServerRequest.TemplateBase64);

                    
                    if (SubjectFromCandidate is null || SubjectFromSavedTemplate is null)
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

                    var Verifystatus = _biometricClient.Verify(SubjectFromSavedTemplate, SubjectFromCandidate);

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

        private NSubject CreateSubjectFromImageBase64(string imageBase64, string id, NBiometricClient biometricClient)
        {
            try
            {
                // Remove data:image/...;base64, if present
                if (imageBase64.Contains(","))
                {
                    imageBase64 = imageBase64.Split(',')[1];
                }

                byte[] imageBytes = Convert.FromBase64String(imageBase64);

                using (var ms = new MemoryStream(imageBytes))
                using (var nstream = NStream.FromStream(ms))
                using (var image = NImage.FromStream(nstream))
                {
                    var finger = new NFinger
                    {
                        Image = image
                    };

                    var subject = new NSubject
                    {
                        Id = id
                    };

                    subject.Fingers.Add(finger);

                    // Create fingerprint template
                    var status =
                        biometricClient.CreateTemplate(subject);

                    if (status != NBiometricStatus.Ok)
                    {
                        throw new Exception(
                            $"Template creation failed: {status}");
                    }

                    return subject;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error creating fingerprint subject from image: {ex.Message}");
            }
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

        private NSubject CreateFingerSubjectFromImage(byte[] imageBytes, string id)
        {
            using (var ms = new MemoryStream(imageBytes))
            using (var nstream = NStream.FromStream(ms))
            using (var image = NImage.FromStream(nstream))
            {
                image.HorzResolution = 500;
                image.VertResolution = 500;

                var finger = new NFinger
                {
                    Image = image,
                    Position = NFPosition.Unknown
                };

                var subject = new NSubject
                {
                    Id = id
                };

                subject.Fingers.Add(finger);

                return subject;
            }
        }

    }
}
