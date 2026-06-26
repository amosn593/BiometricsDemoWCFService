using BiometricsDemo.Helpers;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using Neurotec.Licensing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.Server
{
    public class ServerVoiceService
    {
        public ServerTemplateUpdateResponseModel SaveVoiceTemplate(ServerVoiceModel serverTemplateUpdateModel)
        {
            var Response = new ServerTemplateUpdateResponseModel();
            var updatedTemplateBase64 = string.Empty;
            var DirectoryPath = "C:\\BiometricsServerUAT";
            string FolderPath = Path.Combine(DirectoryPath, "Voice", $"{serverTemplateUpdateModel.PensionerCode}_{serverTemplateUpdateModel.PensionerType}");
            try
            {
                if (string.IsNullOrEmpty(serverTemplateUpdateModel.TemplateBase64))
                {
                    Response.Success = false;
                    Response.Message = "Invalid TemplateBase64!!!";
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
                    Response.Message = $"Could not obtain license: {license}";
                    return Response;
                }

           
                //Save Template
                Directory.CreateDirectory(FolderPath);
                var myTemplatefilename = Path.Combine(FolderPath, serverTemplateUpdateModel.TemplateFilePath);
                var templateBytes = Convert.FromBase64String(serverTemplateUpdateModel.TemplateBase64);
                File.WriteAllBytes(myTemplatefilename, templateBytes);

                
                Response.Success = true;
                Response.UpdatedTemplateBase64 = updatedTemplateBase64;

                return Response;
            }
            catch (Exception ex)
            {
                Response.Success = false;
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                return Response;
            }
        }

        public ServerTemplateUpdateResponseModel VerifyVoiceTemplate(ServerVoiceModel serverTemplateUpdateModel)
        {
            var Response = new ServerTemplateUpdateResponseModel();
            var updatedTemplateBase64 = string.Empty;
            var DirectoryPath = "C:\\BiometricsServerUAT";
            string FolderPath = Path.Combine(DirectoryPath, "Voice", $"{serverTemplateUpdateModel.PensionerCode}_{serverTemplateUpdateModel.PensionerType}");
            try
            {
                if (string.IsNullOrEmpty(serverTemplateUpdateModel.TemplateBase64))
                {
                    Response.Success = false;
                    Response.Message = "Invalid TemplateBase64!!!";
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
                    Response.Message = $"Could not obtain license: {license}";
                    return Response;
                }


                //Save Template
                Directory.CreateDirectory(FolderPath);
                var myTemplatefilename = Path.Combine(FolderPath, serverTemplateUpdateModel.TemplateFilePath);
                var templateBytes = Convert.FromBase64String(serverTemplateUpdateModel.TemplateBase64);
                File.WriteAllBytes(myTemplatefilename, templateBytes);


                Response.Success = true;
                Response.UpdatedTemplateBase64 = updatedTemplateBase64;

                return Response;
            }
            catch (Exception ex)
            {
                Response.Success = false;
                Response.Message = NeuroticErrorMessage.ExtractNeurotecErrorMessage(ex);
                return Response;
            }
        }

    }
}
