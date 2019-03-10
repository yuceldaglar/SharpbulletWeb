using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SharpBullet.Web.System
{
    public class SbApiExceptionHandler
    {
        public virtual void HandleException(HttpContext context, string serviceName, string methodName, string json, Exception exception)
        {
            string error = "";
            string trace = "";

            Exception e = exception;

            error += e.GetBaseException().Message;
            trace += e.GetBaseException().StackTrace;

            SbHandler.ReturnError(context, error, trace);
        }
    }
}