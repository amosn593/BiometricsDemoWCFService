using BiometricsDemo.Enums;
using System.Collections.Generic;

namespace BiometricsDemo.ResponseModels
{
    public class BiometricsResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Biometric Biometric { get; set; } = Biometric.None;
        public string Path { get; set; } = string.Empty;
        public string base64 { get; set; } = string.Empty;
        public string TemplateBase64 { get; set; } = string.Empty;
        public string TemplateFilePath { get; set; } = string.Empty;
        public bool WrongHand { get; set; } = false;
        public List<FingerData> BiometricData { get; set; } = new List<FingerData>();

    }

    public class FingerData
    {
        public string base64 { get; set; } = string.Empty;
        public string filename { get; set; } = string.Empty;
        public string filepath { get; set; } = string.Empty;
        public string quality { get; set; } = string.Empty;
        public string position { get; set; } = string.Empty;
        public int position_int { get; set; } = 0;
    }
}
