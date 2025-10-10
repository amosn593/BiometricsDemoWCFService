using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.RequestModels
{
    public class SaveFaceModel
    {
        public string PensionerCode { get; set; } = string.Empty;
        public int PensionerType { get; set; } = 0;
        public string ImageBase64 { get; set; } = string.Empty;
        public string Imagepath { get; set; } = string.Empty;
        public string TemplateBase64 { get; set; } = string.Empty;
        public string TemplatePath { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }
}
