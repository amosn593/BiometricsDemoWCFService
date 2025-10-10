using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.ResponseModels
{
    public class FaceScanResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string base64image { get; set; } = string.Empty;
        public string fileName { get; set; } = string.Empty;
        public string imagepath { get; set; } = string.Empty;
        public string TemplateBase64 { get; set; } = string.Empty;
        public string TemplatePath { get; set; } = string.Empty;
    }
}
