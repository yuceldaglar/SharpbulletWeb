using Newtonsoft.Json;
using SharpBullet.OAL;
using SharpBullet.Web.Entities.Core;
using SharpBullet.Web.Services;
using SharpBullet.Web.System;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace SharpBullet.Web
{
    public class SbHandler : IHttpHandler, IRequiresSessionState 
    {
        public SbHandler()
        {           
        }

        public void ProcessRequest(HttpContext context)
        {
            var profiler = SbApplication.Current.Create<SbProfiler>();
            profiler.Start("Read stream");

            context.Response.ContentType = "application/json";            
            string service = context.Request.QueryString["api"];
            string json = new StreamReader(context.Request.InputStream).ReadToEnd();
            if (!string.IsNullOrEmpty(context.Request["json"]))
            {
                json = context.Request["json"];
            }

            profiler.Restart("Initialization");

            string serviceMethod = "";
            string serviceName = "";
            string methodName = "";

            var app = SbApplication.Current;
            var user = app.Access.GetUser();

            try                       
            {
                serviceMethod = context.Request["api"];
                if (string.IsNullOrEmpty(serviceMethod))
                    throw new Exception("Parametre eksik: api");

                if (!serviceMethod.Contains("."))
                    throw new Exception("Servis parametresinde metod adı yok.");

                serviceName = serviceMethod.Substring(0, serviceMethod.IndexOf("."));
                methodName = serviceMethod.Substring(serviceMethod.IndexOf(".") + 1);
                
                profiler.Restart("Execute: " + serviceName + "-" + methodName);

                /* magic happens here */
                string result = RunApi(serviceName, methodName, json, context);

                profiler.Restart("Write response. Length: " + (result ?? "").Length);
                if (result != null)
                    context.Response.Write(result);

                profiler.Stop();
                //context.Response.AppendHeader("CustomDebugHeader", JsonConvert.SerializeObject(profiler.Measurements));

                #region log
                try
                {
                    var log = new SbLogApi()
                    {
                        CallTime = profiler.CreateTime,
                        UserId = user != null ? user.Id : 0,
                        Duration = profiler.GetTotalDuration(),
                        Error = false,
                        MethodName = methodName,
                        ServiceName = serviceName,
                        QueryString = context.Request.QueryString.ToString(),
                        JsonText = json
                    };

                    log.Save();

                    //*** Keeping last 1000 log items ***
                    Transaction.Instance.ExecuteNonQuery("delete top(1) from SbLogApi where Id<@prm0", log.Id - 1000);
                }
                catch { }
                #endregion
            }
            catch (Exception exception)
            {
                #region log
                try
                {
                    var e = exception;
                    if (e.GetBaseException() != null) e = e.GetBaseException();

                    var log = new SbLogApi()
                    {
                        CallTime = profiler.CreateTime,
                        UserId = user != null ? user.Id : 0,
                        Duration = profiler.GetTotalDuration(),
                        Error = true,
                        ErrorMessage = e.Message,
                        MethodName = methodName,
                        ServiceName = serviceName,
                        StackTrace = e.StackTrace,
                        QueryString = context.Request.QueryString.ToString(),
                        JsonText = json
                    };

                    log.Save();

                    //*** Keeping last 1000 log items ***
                    Transaction.Instance.ExecuteNonQuery("delete top(1) from SbLogApi where Id<@prm0", log.Id - 1000);
                }
                catch { }
                #endregion

                var exceptionHandler = app.Create<SbApiExceptionHandler>();
                if (exceptionHandler == null) return;

                exceptionHandler.HandleException(context, serviceName, methodName, json, exception);
            }
        }

        public string RunApi(string serviceName, string methodName, string json, HttpContext context)
        {          
            var serviceDictionary = SbApplication.Current.Services;
            Type servisType = null;
            if (!serviceDictionary.ContainsKey(serviceName))
            {                
                var search = serviceName + "Service";
                foreach(var t in this.GetType().BaseType.Assembly.GetTypes())
                {
                    if (t.Name == serviceName || t.Name == search)
                    {
                        serviceDictionary[serviceName] = t;
                        break;
                    }
                }
            }

            if (!serviceDictionary.ContainsKey(serviceName))
                throw new ApplicationException(SbText.Instance.MissingService(serviceName));

            servisType = serviceDictionary[serviceName];            

            MethodInfo methodInfo = servisType.GetMethod(methodName);
            if (methodInfo == null)
                throw new ApplicationException(SbText.Instance.MissingMethod(methodName));

            var access = SbApplication.Current.Access;
            var anonymous = false;

            var attr = methodInfo.GetCustomAttributes(typeof(AnonymousAttribute), true);
            if ((attr != null && attr.Length > 0)
                || access.IsPublicApi(serviceName, methodName))
            {
                anonymous = true;
            }

            if (!anonymous)
            {
                var u = access.GetUser();
                if (u == null || u.NotExist() || !access.HasRight(serviceName, methodName))
                {
                    ReturnAuthorizeError(context);
                    return null;
                }
            }

            object[] parameters = null;
            var parameterInfo = methodInfo.GetParameters();

            if (parameterInfo != null && parameterInfo.Length == 1 && parameterInfo[0].ParameterType == typeof(string))
            {
                parameters = new object[] { json };
            }
            else if (parameterInfo != null && parameterInfo.Length == 1)
            {
                var p = JsonConvert.DeserializeObject(json, parameterInfo[0].ParameterType);
                parameters = new object[] { p };
            }
            else if (parameterInfo != null && parameterInfo.Length > 1)
            {
                throw new ApplicationException(SbText.Instance.OnlyOneParameter(methodName));
            }

            var value = methodInfo.Invoke(null, parameters);
            string result = null;
            if (value != null && value.GetType() == typeof(string))
            {
                result = (string)value;
            } 
            else if (value != null && value.GetType() != typeof(string))
            {
                result = JsonConvert.SerializeObject(value);
            }

            return result;
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        public static void ReturnAuthorizeError(HttpContext context)
        {
            SbResult error = new SbResult
            {
                Success = false,
                ErrorMessage = "notauthorized"
            };

            string json = JsonConvert.SerializeObject(error);

            context.Response.TrySkipIisCustomErrors = true;
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 400; /* 401 kullanma, redirect'e sebep oluyor (iis'in özeliği) */
            context.Response.Write(json);
        }

        public static void ReturnError(HttpContext context, string errorMessage, string stackTrace)
        {
            SbResult error = new SbResult
            {
                Success = false,
                ErrorMessage = errorMessage.Replace('"', '\'').Replace("\r\n", " "),
                Value = stackTrace.Replace('"', '\'').Replace("\r\n", " ")
            };

            string json = JsonConvert.SerializeObject(error);

            context.Response.TrySkipIisCustomErrors = true;
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 403;//? 400;
            context.Response.Write(json);
        }

        public static void ReturnSuccess2(HttpContext context, object resultObject)
        {
            SbResult success = new SbResult
            {
                Success = true,
                Data = resultObject
            };

            string json = JsonConvert.SerializeObject(success, new JsonSerializerSettings() { MaxDepth = 1 });

            context.Response.ContentType = "application/json";
            context.Response.Write(json);
        }

        public static void ReturnSuccess(HttpContext context, object resultObject)
        {
            SbResult success = new SbResult
            {
                Success = true,
                Value = (resultObject ?? "").ToString()
            };

            string json = JsonConvert.SerializeObject(success);

            context.Response.ContentType = "application/json";
            context.Response.Write(json);
        }
    }
}
