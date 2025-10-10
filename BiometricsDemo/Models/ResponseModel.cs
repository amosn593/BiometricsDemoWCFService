using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricsDemo.Models
{
    public class ResponseModel<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "Successful";
        public T Data { get; set; } 
    }
}
