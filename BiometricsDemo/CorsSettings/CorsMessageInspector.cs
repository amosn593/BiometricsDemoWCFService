using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

namespace BiometricsDemo.CorsSettings
{
    public class CorsMessageInspector : IDispatchMessageInspector
    {
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            if (reply == null) return;

            HttpResponseMessageProperty httpResponse;
            if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
            {
                httpResponse = (HttpResponseMessageProperty)reply.Properties[HttpResponseMessageProperty.Name];
            }
            else
            {
                httpResponse = new HttpResponseMessageProperty();
                reply.Properties.Add(HttpResponseMessageProperty.Name, httpResponse);
            }

            // Get Origin and requested headers from incoming request
            string origin = null; // null;
            string requestedHeaders = null;

            if (OperationContext.Current.IncomingMessageProperties.ContainsKey(HttpRequestMessageProperty.Name))
            {
                var httpRequest = (HttpRequestMessageProperty)OperationContext.Current.IncomingMessageProperties[HttpRequestMessageProperty.Name];
                origin = httpRequest.Headers["Origin"];
                requestedHeaders = httpRequest.Headers["Access-Control-Request-Headers"];
            }

            // Always allow origin (or restrict in prod)
            httpResponse.Headers["Access-Control-Allow-Origin"] = string.IsNullOrEmpty(origin) ? "*" : origin;
            httpResponse.Headers["Vary"] = "Origin";

            httpResponse.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";

            // Mirror whatever headers the browser asked for
            if (!string.IsNullOrEmpty(requestedHeaders))
                httpResponse.Headers["Access-Control-Allow-Headers"] = requestedHeaders;
            else
                httpResponse.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        }
    }

}
