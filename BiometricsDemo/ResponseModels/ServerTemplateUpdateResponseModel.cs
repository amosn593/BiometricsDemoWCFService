using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.ResponseModels
{
    public class ServerTemplateUpdateResponseModel
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public string UpdatedTemplateBase64 { get; set; } = string.Empty;
    }
}
