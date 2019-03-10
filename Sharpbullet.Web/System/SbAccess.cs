using SharpBullet.Web.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SharpBullet.Web.System
{
    public class SbAccess
    {
        private const string USER_KEY = "SbUserKey";

        private SbAccessConfiguration configuration;

        public SbAccessConfiguration Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new SbAccessConfiguration();
                    configuration.Initialize();
                }
                return configuration;
            }
            set { configuration = value; }
        }





        public SbUser GetUser()
        {
            var user = HttpContext.Current.Session[USER_KEY] as SbUser
                ?? new SbUser();

            return user;
        }

        public void SetUser(SbUser user)
        {
            HttpContext.Current.Session[USER_KEY] = user;
        }

        public bool HasRight(string serviceName, string methodName)
        {
            //TODO implement has-right
            return true;
        }

        public bool IsPublicApi(string serviceName, string methodName)
        {
            var apiList = Configuration.PublicApi.Split(',');
            if (apiList.Contains(serviceName + ".*")
                || apiList.Contains(serviceName + "." + methodName)) return true;

            return false;
        }
    }

    public class SbAccessConfiguration : SbConfiguration
    {
        public string Salt { get; set; }

        public string PublicHomePage { get; set; }

        public string MemberHomePage { get; set; }

        public string LoginPage { get; set; }

        /// <summary>
        /// Comman seperated list of api, example usage login-signup api
        /// </summary>
        public string PublicApi { get; set; }





        public SbAccessConfiguration()
        {
            Salt = "48ghswytf7ryf83td5r";
        }
    }
}
