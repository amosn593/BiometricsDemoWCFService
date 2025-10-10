using BiometricsDemo.Client;
using BiometricsDemo.Models;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using BiometricsDemo.Server;
using Neurotec.Licensing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace BiometricsDemo.APIs
{
    public class ServiceMain : IServiceMain
    {
        private static readonly WebSocketManager wsManager = new WebSocketManager();

        public string TestService()
        {
            return "Testing Service";
        }

        public Person AddPerson(Person person)
        {
            Console.WriteLine($"Received: Name={person.Name}, Age={person.Age}");

            // Modify before returning
            person.Name = person.Name.ToUpper();
            person.Age += 1;

            return person;
        }
        public Person GetPerson()
        {
            var person = new Person
            {
                Name = "John Doe",
                Age = 30
            };
            Console.WriteLine($"Returned: Name={person.Name}, Age={person.Age}");


            return person;
        }
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

                const string license = "FingerClient,FingerMatcher";

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

        public List<CameraListResponseModel> getCameraList()
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
    }
}
