using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace SharpBullet.Web.System
{
    public class SbControlTemplate
    {
        private SbControlTemplateConfiguration configuration;

        public SbControlTemplateConfiguration Configuration
        {
            get
            {
                if (configuration == null) configuration = new SbControlTemplateConfiguration();
                return configuration;
            }
            set { configuration = value; }
        }

        public string GetTemplate<T>()
        {
            return GetTemplate(typeof(T).Name);
        }

        public string GetTemplate(string typeName)
        {
            var root = HttpContext.Current.Server.MapPath(Configuration.TemplateRoot);
            if (!root.EndsWith("/")) root += "/";

            //TODO cache template
            var templateFile = root + typeName + Configuration.Extension;
            if (File.Exists(templateFile))
                return File.ReadAllText(templateFile);

            return "";
        }
    }

    public class SbControlTemplateConfiguration
    {
        public string Extension { get; set; }
        public string TemplateRoot { get; set; }

        public SbControlTemplateConfiguration()
        {
            Extension = ".html";
            TemplateRoot = "/Assets/Controls/";
        }
    }
}
