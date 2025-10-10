using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.ResponseModels
{
    public class FingerPrintVerifyResponse
    {
        public bool Success { get; set; }
        public bool Match { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
