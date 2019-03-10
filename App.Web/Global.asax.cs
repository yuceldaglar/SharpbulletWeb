using SharpBullet.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace App.Web
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            #region Default Routes
            var root = Server.MapPath("/");
            var files = System.IO.Directory.GetFiles(root, "*.aspx", System.IO.SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string physicalFile = file.Replace('\\', '/').Substring(root.Length);
                string routeUrl = physicalFile.Substring(0, physicalFile.Length - 5);

                RouteTable.Routes.MapPageRoute(routeUrl, routeUrl, "~/" + physicalFile);
            }
            #endregion
        }
    }
}