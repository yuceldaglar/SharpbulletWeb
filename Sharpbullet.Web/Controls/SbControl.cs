using SharpBullet.Web.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web.Controls
{
    public class SbControl
    {
        public string Id { get; set; }
        public string Class { get; set; }

        public virtual string Render(SbApplication app)
        {
            var template = app.Create<SbControlTemplate>();
            return TemplateRenderer(
                template.GetTemplate(this.GetType().Name),
                GetValues()
            );
        }

        protected virtual Dictionary<string, object> GetValues()
        {
            return new Dictionary<string, object>()
            {
                { "Id", Id },
                { "Class", Class }
            };
        }

        public static string TemplateRenderer(string template, string key, object value)
        {
            return TemplateRenderer(template, new Dictionary<string, object>()
            {
                { key, value }
            });
        }

        public static string TemplateRenderer(string template, Dictionary<string, object> values)
        {
            var result = template;
            if (values == null && values.Count == 0) return result;

            foreach (var item in values)
            {
                var key = "$" + item.Key + "$";
                result = result.Replace(key, (item.Value ?? "").ToString());
            }
            return result;
        }
    }
}
