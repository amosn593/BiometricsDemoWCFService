using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    // Run in console for debugging
                    var service = new WcfWindowsService();
                    service.StartDebug(args);

                    //Console.WriteLine("BioMetrics Service running at http://localhost:9191/Service/");
                    Console.WriteLine("Press any key to stop...");
                    Console.ReadKey();

                    service.StopDebug();
                }
                else
                {
                    // Run as a real Windows Service
                    ServiceBase.Run(new WcfWindowsService());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return;
            }
        }
    }
}
