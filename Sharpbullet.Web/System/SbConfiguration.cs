using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Configuration;

namespace SharpBullet.Web.System
{
    public class SbConfiguration
    {
        public void Initialize()
        {
            var type = this.GetType();
            var props = type.GetProperties();
            foreach (var prop in props)
            {
                var key = type.Name + "-" + prop.Name;
                var value = WebConfigurationManager.AppSettings[key];
                if (string.IsNullOrEmpty(value)) continue;

                var propValue = Convert.ChangeType(value, prop.PropertyType);

                prop.SetValue(this, propValue);
            }
        }
    }
}