using BiometricsDemo.CorsSettings;
using BiometricsDemo.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using BiometricsDemo.APIs;

namespace BiometricsDemo
{
    public class WcfWindowsService : ServiceBase
    {
        private ServiceHost _host;
        private readonly Uri _baseUri = new Uri("http://localhost:8181/Service/");

        public WcfWindowsService()
        {
            ServiceName = "BioMetricsService";
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                StartServiceHost();
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnStart failed", ex);
                throw; // let Windows know startup failed
            }
        }

        protected override void OnStop()
        {
            try
            {
                StopServiceHost();
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnStop failed", ex);
            }
        }

        //Public wrappers for console/debug mode
        public void StartDebug(string[] args)
        {
            try
            {
                StartServiceHost();
            }
            catch (Exception ex)
            {
                Console.WriteLine("StartDebug failed", ex);
            }
        }

        public void StopDebug()
        {
            try
            {
                StopServiceHost();
            }
            catch (Exception ex)
            {
                Console.WriteLine("StopDebug failed", ex);
            }
        }

        private void StartServiceHost()
        {
            try
            {
                if (_host != null)
                {
                    _host.Close();
                }

                _host = new ServiceHost(typeof(ServiceMain), _baseUri);

                var binding = new WebHttpBinding
                {
                    MaxBufferSize = int.MaxValue,
                    MaxReceivedMessageSize = int.MaxValue,
                    TransferMode = TransferMode.Buffered,
                    ReaderQuotas =
                    {
                        MaxDepth = int.MaxValue,
                        MaxStringContentLength = int.MaxValue,
                        MaxArrayLength = int.MaxValue,
                        MaxBytesPerRead = int.MaxValue,
                        MaxNameTableCharCount = int.MaxValue
                    }
                };

                var endpoint = _host.AddServiceEndpoint(
                    typeof(IServiceMain),
                    binding,
                    ""
                );

                endpoint.EndpointBehaviors.Add(new WebHttpBehavior { HelpEnabled = true });
                endpoint.EndpointBehaviors.Add(new CorsBehavior());

                _host.Open();
            }
            catch (AddressAccessDeniedException ex)
            {
                Console.WriteLine("Access denied to port. Did you reserve the URL with netsh?", ex);
                throw;
            }
            catch (AddressAlreadyInUseException ex)
            {
                Console.WriteLine("Port already in use. Another service may be listening.", ex);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error while starting ServiceHost.", ex);
                throw;
            }

        }

        private void StopServiceHost()
        {
            try
            {
                if (_host != null)
                {
                    _host.Close();
                    _host = null;
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while stopping ServiceHost.", ex);
            }
        }
    }
}
