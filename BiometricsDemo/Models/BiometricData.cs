using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.Models
{
    //DataContract]
    public class BiometricData
    {
       // [DataMember]
        public string DeviceId { get; set; }

        //DataMember]
        public byte[] ScanBytes { get; set; }

        //[DataMember]
        public string ScanType { get; set; }
    }


    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
