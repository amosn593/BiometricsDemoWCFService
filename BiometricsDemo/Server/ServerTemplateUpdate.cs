using BiometricsDemo.Helpers;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using Neurotec.Biometrics;
using Neurotec.Licensing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.Server
{
    public class ServerTemplateUpdate
    {
        public ServerTemplateUpdateResponseModel UpdateTemplate(ServerTemplateUpdateModel serverTemplateUpdateModel)
        {
            var Response = new ServerTemplateUpdateResponseModel();
            var updatedTemplateBase64 = string.Empty;
            var DirectoryPath = "C:\\BiometricsServerUAT";
            string FolderPath = Path.Combine(DirectoryPath, "FingerPrints", $"{serverTemplateUpdateModel.PensionerCode}_{serverTemplateUpdateModel.PensionerType}");
            try
            {
                if (string.IsNullOrEmpty(serverTemplateUpdateModel.Base64) || string.IsNullOrEmpty(serverTemplateUpdateModel.NewTemplateBase64))
                {
                    Response.Success = false;
                    Response.Message = "Invalid ImageBase64 or TemplateBase64!!!";
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

                //Save Image
                Directory.CreateDirectory(FolderPath);
                var myBaseImagefilename = Path.Combine(FolderPath, serverTemplateUpdateModel.Path);
       
                var BaseImagebytes = Convert.FromBase64String(serverTemplateUpdateModel.Base64);
               
                File.WriteAllBytes(myBaseImagefilename, BaseImagebytes);

                //Save Template
                var myTemplatefilename = Path.Combine(FolderPath, serverTemplateUpdateModel.TemplateFilePath);

                if (!File.Exists(myTemplatefilename))
                {
                    var templateBytes = Convert.FromBase64String(serverTemplateUpdateModel.NewTemplateBase64);
                    File.WriteAllBytes(myTemplatefilename, templateBytes);
                    updatedTemplateBase64 = serverTemplateUpdateModel.NewTemplateBase64;
                }
                else
                {
                    //Read existing template
                    var existingTemplateBytes = File.ReadAllBytes(myTemplatefilename);
                    serverTemplateUpdateModel.OldTemplateBase64 = Convert.ToBase64String(existingTemplateBytes);
                    // Convert Base64 strings to byte arrays
                    updatedTemplateBase64 = ServerUpdateMasterTemplate(serverTemplateUpdateModel.OldTemplateBase64, serverTemplateUpdateModel.NewTemplateBase64);
                    var UpdatedtemplateBytes = Convert.FromBase64String(updatedTemplateBase64);
                    File.WriteAllBytes(myTemplatefilename, UpdatedtemplateBytes);

                }


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

        private static string ServerUpdateMasterTemplate(string OldTemplateBase64, string NewTemplateBase64)
        {
            // Generate template from the captured subject
            var newTemplateBytes = Convert.FromBase64String(NewTemplateBase64);
            var newTemplate = new NTemplate(newTemplateBytes);

            
            // Generate Old master
            var masterBytes = Convert.FromBase64String(OldTemplateBase64);
            var masterTemplate = new NTemplate(masterBytes);

            // Ensure master has a finger section
            if (masterTemplate.Fingers == null)
                masterTemplate.Fingers = new NFTemplate();

            // Merge new fingers if available
            if (newTemplate.Fingers != null && newTemplate.Fingers.Records.Count > 0)
            {
                var masterRecords = masterTemplate.Fingers.Records;  //NFTemplate.Records
                var newRecords = newTemplate.Fingers.Records;

                var existingPositions = new HashSet<NFPosition>(
                    masterRecords.Select(r => r.Position));

                foreach (var rec in newRecords)
                {
                    if (!existingPositions.Contains(rec.Position))
                    {
                        // Clone NFRecord to avoid ownership issues
                        var cloned = new NFRecord(rec.Save().ToArray());
                        masterRecords.Add(cloned);
                    }
                }
            }

            //Returned updated master template
            var NewMsaterTemplateBase64 = Convert.ToBase64String(masterTemplate.Save().ToArray());
            return NewMsaterTemplateBase64;
        }

    }
}
