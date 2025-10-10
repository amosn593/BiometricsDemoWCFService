namespace BiometricsDemo.RequestModels
{
    public class BiometricsRequest
    {
        public string PensionerCode { get; set; }//0
        public int PensionerType { get; set; }//1
        public int? RegionCode { get; set; }//2                                  
        public int Mode { get; set; }//3
        public int Finger { get; set; } //6                                      
        public int?[] MissingFingers { get; set; }//7
        public int UserId { get; set; }//8
        
    }
}
