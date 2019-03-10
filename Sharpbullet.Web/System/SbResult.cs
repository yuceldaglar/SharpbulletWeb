using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBullet.Web
{
    public class SbResult
    {
        public bool Success { get; set; }
        public string ErrorId { get; set; }
        public string ErrorMessage { get; set; }
        public string Value { get; set; }
        public object Data { get; set; }

        public SbResult()
        {
            ErrorId = ErrorMessage = Value = "";
        }

        public static SbResult GetSuccess(string value = "")
        {
            return new SbResult() { Success = true, Value = value };
        }
    }
}