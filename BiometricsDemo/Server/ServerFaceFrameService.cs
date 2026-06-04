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

namespace BiometricsDemo.Server
{
    public class ServerFaceFrameService
    {
        public SaveFaceModel SaveFace(SaveFaceModel saveFaceModel)
        {
            var Response = new SaveFaceModel();
            try
            {
                var DirectoryPath = "C:\\BiometricsServer";
                string FolderPath = Path.Combine(DirectoryPath, "FaceFrames", $"{saveFaceModel.PensionerCode}_{saveFaceModel.PensionerType}");

                if (string.IsNullOrEmpty(saveFaceModel.PensionerCode) || saveFaceModel.PensionerType <= 0 || string.IsNullOrEmpty(saveFaceModel.ImageBase64))
                {
                    Response.Success = false;
                    Response.Message = "Invalid Data Saving FaceFrames at Biometrics Service";
                    return Response;
                }

                //Save Image
                Directory.CreateDirectory(FolderPath);
                var myBaseImagefilename = Path.Combine(FolderPath, $"{saveFaceModel.Imagepath}");
                File.WriteAllBytes(myBaseImagefilename, Convert.FromBase64String(saveFaceModel.ImageBase64));

                //Save Template
                if(!string.IsNullOrEmpty(saveFaceModel.TemplateBase64))
                {
                    var myTemplatefilename = Path.Combine(FolderPath, $"{saveFaceModel.TemplatePath}");
                    File.WriteAllBytes(myTemplatefilename, Convert.FromBase64String(saveFaceModel.TemplateBase64));
                }

                Response.Success = true;
                Response.Message = "FaceFrame Saved Successfully at Biometrics Service";
                return Response;

            }
            catch (Exception ex)
            {
                Response.Success = false;
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                return Response;
            }
        }

        public ServerRequestResponse VerfiyFace(SaveFaceModel saveFaceModel)
        {
            var Response = new ServerRequestResponse();
            try
            {
                var DirectoryPath = "C:\\BiometricsServer";
                string FolderPath = Path.Combine(DirectoryPath, "FaceFrames", $"{saveFaceModel.PensionerCode}_{saveFaceModel.PensionerType}");

                if (string.IsNullOrEmpty(saveFaceModel.PensionerCode) || saveFaceModel.PensionerType <= 0 )
                {
                    Console.WriteLine("Invalid Data Verifying FaceFrames at Biometrics Service");
                    Response.Success = false;
                    Response.Message = "Invalid Data Verifying FaceFrames at Biometrics Service";
                    Response.ErrorMsg = "Invalid PensionerCode or PensionerType or Biometric!!!";
                    Response.Match = false;
                    return Response;
                }

                //const string license = "FaceMatcher";
                //const string license = "FaceClient";
                //const string license = "FaceFastExtractor";
                //const string license = "SentiVeillance";

                const string license = "FaceClient,FaceMatcher";

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
                    Response.Message = "Could not obtain license.";
                    Response.Status = "UnVerified";
                    Response.Match = false;
                    return Response;
                }


               
                var myTemplatefilename = Path.Combine(FolderPath, $"{saveFaceModel.Imagepath}");

                Console.WriteLine($"Template Path: {myTemplatefilename}");

                if (!File.Exists(myTemplatefilename))
                {
                    Console.WriteLine("No FaceFrame Template Found at Biometrics Service");
                    Response.Success = false;
                    Response.Match = false;
                    Response.Message = "No FaceFrame Template Found at Biometrics Service";
                    Response.ErrorMsg = "No FaceFrame Template Found at Biometrics Service";
                    return Response;
                }

                //Load the saved Image
                var savedImageBytes = File.ReadAllBytes(myTemplatefilename);

                byte[] imageBytes = Convert.FromBase64String(saveFaceModel.ImageBase64);
                
                using (var _biometricClient = new NBiometricClient { })
                {
                    using (var subject1 = CreateSubjectFromImage(savedImageBytes, "subject1"))
                    using (var subject2 = CreateSubjectFromImage(imageBytes, "subject2"))
                    {
                        // Generate templates
                        var status1 = _biometricClient.CreateTemplate(subject1);
                        var status2 = _biometricClient.CreateTemplate(subject2);

                        if (status1 != NBiometricStatus.Ok || status2 != NBiometricStatus.Ok)
                        {
                            Console.WriteLine($"Template creation failed. Status1={status1}, Status2={status2}");
                            Response.Success = false;
                            Response.ErrorMsg = $"Template creation failed. Status1={status1}, Status2={status2}";
                            Response.Message = $"Template creation failed. Status1={status1}, Status2={status2}";
                            Response.Status = "UnVerified";
                            Response.Match = false;
                            return Response;
                        }

                        _biometricClient.MatchingThreshold = 90;

                        _biometricClient.FacesMatchingSpeed = NMatchingSpeed.Medium;

                        // Compare the two templates
                        var Verifystatus = _biometricClient.Verify(subject1, subject2);

                        if (Verifystatus == NBiometricStatus.Ok)
                        {
                            Response.Success = true;
                            Response.ErrorMsg = $"Verification success";
                            Response.Message = $"Verification success";
                            Response.Status = "Verified";
                            Response.Match = true;
                            return Response;
                        }
                        Console.WriteLine($"Verification failed. Status={Verifystatus}");
                        Response.Success = false;
                        Response.ErrorMsg = $"Verification unsuccess";
                        Response.Message = "FaceFrames did not match.";
                        Response.Status = "UnVerified";
                        Response.Match = false;
                        return Response;
                    }
                }

            }
            
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error in Verifying FaceFrames at Biometrics Service: {ex.Message}");
                Response.Success = false;
                Response.Match = false;
                Response.ErrorMsg = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                return Response;
            }
        }

        private NSubject CreateSubjectFromImage(byte[] imageBytes, string id)
        {
            using (var ms = new MemoryStream(imageBytes))
            using (var nstream = NStream.FromStream(ms)) //wrap MemoryStream
            using (var image = NImage.FromStream(nstream))
            {
                var face = new NFace { Image = image };
                var subject = new NSubject { Id = id };
                subject.Faces.Add(face);
                return subject;
            }
        }

       
    }
}
