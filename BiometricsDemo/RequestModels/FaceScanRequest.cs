using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.RequestModels
{
    public class FaceScanRequest
    {
        public int? CameraIndex { get; set; } = null;
        public string CameraName { get; set; } = string.Empty;
        public string PensionerCode { get; set; }//0
        
        public int PensionerType { get; set; }//1
        
        public int? RegionCode { get; set; }//2
        public int UserId { get; set; }//8
    }
}
