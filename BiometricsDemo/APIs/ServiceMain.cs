using BiometricsDemo.Client;
using BiometricsDemo.Models;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using BiometricsDemo.Server;
using Neurotec.Licensing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace BiometricsDemo.APIs
{
    public class ServiceMain : IServiceMain
    {
        private static readonly WebSocketManager wsManager = new WebSocketManager();

        public async Task<FaceScanResponse> FaceCapture(FaceScanRequest faceScanRequest)
        {
            try
            {
                var bioMetrics = new FaceCameraService();
                var FaceCaptureResults = await bioMetrics.SaveLastFrame(faceScanRequest);
                //wsManager.Stop();
                return FaceCaptureResults;
            }
            catch (Exception ex)
            {
                //wsManager.Stop();
                return new FaceScanResponse
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorMessage = ex.Message,
                    base64image = string.Empty
                };
            }
        }
        public ResponseModel<string> StartWebSocket()
        {
            try
            {
                wsManager.Start();
                return new ResponseModel<string>
                {
                    Data = "WebSocket server started."
                };

            }
            catch (Exception ex)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = ex.ToString()
                };
            }

        }
        public ResponseModel<string> StopWebSocket()
        {
            try
            {
                wsManager.Stop();
                return new ResponseModel<string>
                {
                    Data = "WebSocket server stopped."
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = ex.ToString()
                };
            }

        }
        public void Options()
        {
            // No body needed; headers are injected by the inspector
        }
        public async Task<ServerRequestResponse> VerifyFingerPrints(VerifyServerRequest verifyServerRequest)
        {
            try
            {
                var bioMetrics = new ServerFingerPrintVerifyService();

                var FingerPrintVerifyResults = await bioMetrics.VerifyFingerPrint(verifyServerRequest);

                return FingerPrintVerifyResults;
            }
            catch (Exception ex)
            {
                return new ServerRequestResponse
                {
                    Success = false,
                    ErrorMsg = ex.Message,
                    Status = "UnVerified",
                    Match = false
                };
            }
        }
        public async Task<ServerTemplateUpdateResponseModel> UpdateMasterTemplate(ServerTemplateUpdateModel serverTemplateUpdateModel)
        {
            try
            {
                var templateUpdate = new ServerTemplateUpdate();

                var TemplateUpdateResults = templateUpdate.UpdateTemplate(serverTemplateUpdateModel);

                return TemplateUpdateResults;
            }
            catch (Exception ex)
            {
                return new ServerTemplateUpdateResponseModel
                {
                    Success = false,
                    Message = ex.Message,
                    UpdatedTemplateBase64 = string.Empty
                };
            }
        }
        public async Task<SaveFaceModel> SaveFaceFrame(SaveFaceModel saveFaceModel)
        {
            try
            {
                var faceFrameService = new ServerFaceFrameService();
                var SaveFaceResults = faceFrameService.SaveFace(saveFaceModel);
                return SaveFaceResults;
            }
            catch (Exception ex)
            {
                return new SaveFaceModel
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<ServerRequestResponse> VerifyFaceFrame(SaveFaceModel saveFaceModel)
        {
            try
            {
                var faceFrameService = new ServerFaceFrameService();
                var VerifyFaceResults = faceFrameService.VerfiyFace(saveFaceModel);
                return VerifyFaceResults;
            }
            catch (Exception ex)
            {
                return new ServerRequestResponse
                {
                    Success = false,
                    Match = false,
                    Message = ex.Message,
                    ErrorMsg = ex.Message
                    
                };
            }
        }
        public string GetLicense()
        {
            try
            {
                if (NLicenseManager.TrialMode == false)
                {
                    //NLicense.Release(license);
                    NLicenseManager.TrialMode = true;// GetTrialModeFlag();
                    Console.WriteLine("Trial mode: " + NLicenseManager.TrialMode);
                }

                const string license = "FingerClient,FingerMatcher,FaceClient,FaceMatcher";

                if (!NLicense.Obtain("/local", 5000, license))
                {

                    return $"Could not obtain license: {license}";
                }

                return $"Obtained license: {license}";
            }
            catch (Exception ex)
            {
                return $"Could not obtain license: {ex.Message}";
            }
        }
        public List<CameraListResponseModel> GetCameraList()
        {
            try
            {
                var CameraService = new FaceCameraService();
                var cameraList = CameraService.GetConnectedCameras();
                return cameraList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in geCameraList: {ex.Message}");
                return new List<CameraListResponseModel>();
            }
        }
        public ResponseModel<string> DeleteFaceFrameData(string path)
        {
            try
            {
                var DirectoryPath = "C:\\BiometricsServer";
                string FolderPath = Path.Combine(DirectoryPath, "FaceFrames", $"{path}");

                if (Directory.Exists(FolderPath))
                {
                    Directory.Delete(FolderPath, recursive: true);
                    Console.WriteLine($"Deleted  FaceFrames folder: {FolderPath}");
                    return new ResponseModel<string>
                    {
                        Success = true,
                        Message = $"Deleted FaceFrames folder: {FolderPath}",
                        Data = FolderPath
                    };
                }
                else
                {
                    Console.WriteLine($"Folder FaceFrames not found: {FolderPath}");
                    return new ResponseModel<string>
                    {
                        Success = true,
                        Message = $"Folder FaceFrames not found: {FolderPath}",
                        Data = FolderPath
                    };
                }

            }
            catch (Exception ex)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = ex.ToString()
                };
            }
        }
        public ResponseModel<string> DeleteFingerPrintData(string path)
        {
            try
            {
                var DirectoryPath = "C:\\BiometricsServer";
                string FolderPath = Path.Combine(DirectoryPath, "FingerPrints", $"{path}");

                if (Directory.Exists(FolderPath))
                {
                    Directory.Delete(FolderPath, recursive: true);
                    Console.WriteLine($"Deleted FingerPrints folder: {FolderPath}");
                    return new ResponseModel<string>
                    {
                        Success = true,
                        Message = $"Deleted FingerPrints folder: {FolderPath}",
                        Data = FolderPath
                    };
                }
                else
                {
                    Console.WriteLine($"Folder FingerPrints not found: {FolderPath}");
                    return new ResponseModel<string>
                    {
                        Success = true,
                        Message = $"Folder FingerPrints not found: {FolderPath}",
                        Data = FolderPath
                    };
                }

            }
            catch (Exception ex)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = ex.ToString()
                };
            }
        }
    }
}
