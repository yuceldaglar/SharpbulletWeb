using SharpBullet.Web.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web.Controls
{
    public class SbMenu : SbControl
    {
        public override string Render(SbApplication app)
        {
            var template = app.Create<SbControlTemplate>();
            var values = GetValues();
            values["Content"] = RenderMenuItems(template, app.MainMenu.MenuItems);

            return TemplateRenderer(
                template.GetTemplate(this.GetType().Name),
                values
            );
        }

        private string RenderMenuItems(SbControlTemplate template, List<SbMainMenuItem> menuItems)
        {
            var result = "";
            var values = new Dictionary<string, object>();
            foreach (var m in menuItems)
            {
                if (!string.IsNullOrEmpty(m.Link))
                {
                    values["Link"] = m.Link;
                    values["Text"] = m.Text;
                    result += TemplateRenderer(
                                    template.GetTemplate(this.GetType().Name + "_Link"),
                                    values
                                );
                }
                else if (string.IsNullOrEmpty(m.Link) && m.SubMenus.Count == 0)
                {
                    /* empty node */
                }
                else
                {
                    values["Text"] = m.Text;
                    values["Content"] = RenderMenuItems(template, m.SubMenus);
                    result += TemplateRenderer(
                                    template.GetTemplate(this.GetType().Name + "_Folder"),
                                    values
                                );
                }
            }
            return result;
        }
    }
}