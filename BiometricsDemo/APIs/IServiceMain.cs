using BiometricsDemo.Models;
using BiometricsDemo.RequestModels;
using BiometricsDemo.ResponseModels;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;

namespace BiometricsDemo.APIs
{
    [ServiceContract]
    public interface IServiceMain
    {
        [OperationContract]
        [WebInvoke(Method = "GET",
                 UriTemplate = "/test",
                 RequestFormat = WebMessageFormat.Json,
                 ResponseFormat = WebMessageFormat.Json,
                 BodyStyle = WebMessageBodyStyle.Bare)]
        string Test();

        [OperationContract]
        [WebInvoke(Method = "GET",
                UriTemplate = "/getLicense",
                RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        string GetLicense();

        [OperationContract]
        [WebGet(
                UriTemplate = "/deleteFaceFrameData?path={path}",
                //RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        ResponseModel<string> DeleteFaceFrameData(string path);

        [OperationContract]
        [WebGet(
                UriTemplate = "/deleteFingerPrintData?path={path}",
                //RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        ResponseModel<string> DeleteFingerPrintData(string path);

        [OperationContract]
        [WebInvoke(Method = "GET",
                UriTemplate = "/getCameraList",
                RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        List<CameraListResponseModel> GetCameraList();

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/startWebSocket",
                RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        ResponseModel<string> StartWebSocket();

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/stopWebSocket",
                RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        ResponseModel<string> StopWebSocket();

        [OperationContract]
        [WebInvoke(Method = "POST",
                UriTemplate = "/facecapture",
                RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        Task<FaceScanResponse> FaceCapture(FaceScanRequest faceScanRequest);

        [OperationContract]
        [WebInvoke(Method = "POST",
                UriTemplate = "/verifyFingerPrints",
                RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        Task<ServerRequestResponse> VerifyFingerPrints(VerifyServerRequest verifyServerRequest);

        [OperationContract]
        [WebInvoke(Method = "POST",
                UriTemplate = "/updateMasterTemplate",
                RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        Task<ServerTemplateUpdateResponseModel> UpdateMasterTemplate(ServerTemplateUpdateModel serverTemplateUpdateModel);

        [OperationContract]
        [WebInvoke(Method = "POST",
                UriTemplate = "/saveFaceFrame",
                RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        Task<SaveFaceModel> SaveFaceFrame(SaveFaceModel saveFaceModel);

        [OperationContract]
        [WebInvoke(Method = "POST",
                UriTemplate = "/veryFaceFrame",
                RequestFormat = WebMessageFormat.Json,
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.Bare)]
        Task<ServerRequestResponse> VerifyFaceFrame(SaveFaceModel saveFaceModel);

        //Catch-all OPTIONS fallback(only hit if inspector didn’t already short-circuit)
        [OperationContract]
        [WebInvoke(Method = "OPTIONS", UriTemplate = "*")]
        void Options();
    }
}
