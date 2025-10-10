using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.ResponseModels
{
    public class ServerRequestResponse
    {
        public string ErrorMsg { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public bool Match { get; set; }
        public string Status { get; set; }
        public string Id { get; set; }
    }
}
