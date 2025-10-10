using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.RequestModels
{
    public class VerifyServerRequest
    {
        public int cyclesId { get; set; }
        public int userId { get; set; }
        public int placeCensus { get; set; }
        public int pensionerType { get; set; }
        public int pensionerCode { get; set; }
        public int verificationmode { get; set; }
        public string TemplateBase64 { get; set; } = string.Empty;
        public string TemplateFilePath { get; set; } = string.Empty;
        public string ErrorMsg { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
       
    }

    public class SavedImages
    {
        public bool Success { get; set; }
        public string ImagePath { get; set; }
        public string ImageBase64 { get; set; }
    }
}
