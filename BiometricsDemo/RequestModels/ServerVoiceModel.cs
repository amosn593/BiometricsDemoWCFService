using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.RequestModels
{
    public class ServerVoiceModel
    {
        public string TemplateBase64 { get; set; } = string.Empty;
        public string PensionerCode { get; set; } = string.Empty;
        public int PensionerType { get; set; }
        public string Base64 { get; set; } = string.Empty;
        public string TemplateFilePath { get; set; } = string.Empty;
    }
}
