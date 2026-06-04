using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.RequestModels
{
    public class FingerPrintVerifyRequest
    {
        public int CycleId { get; set; }
        public int PlaceCensus { get; set; }
        public string PensionerCode { get; set; }
        public int PensionerType { get; set; }
        public string TemplateBase64 { get; set; }
        public int? Mode { get; set; }
        public int Finger { get; set; }
        public int UserId { get; set; } = 0;
        public bool IsOneFingerScanner { get; set; }
    }
}
