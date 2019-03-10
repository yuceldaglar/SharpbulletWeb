using SharpBullet.Web.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web.Controls
{
    public class SbForm
    {
        private List<SbControl> controls;

        public List<SbControl> Controls
        {
            get
            {
                if (controls == null) controls = new List<SbControl>();
                return controls;
            }
        }

        private List<SbScript> scripts;

        public List<SbScript> Scripts
        {
            get { if (scripts == null) scripts = new List<SbScript>(); return scripts; }
            set { scripts = value; }
        }





        public SbForm Add(params SbControl[] control)
        {
            Controls.AddRange(control);
            return this;
        }

        public SbForm Add(string name, string script)
        {
            Scripts.Add(new SbScript() { Name = name, Script = script }); 
            return this;
        }

        public string Render()
        {
            var html = "";
            if (Controls == null || Controls.Count == 0) return html;

            var app = SbApplication.Current;
            foreach (var control in Controls)
            {
                html += control.Render(app) + "\r\n";
            }

            html += "\r\n";

            var script = RenderScripts();

            var template = SbApplication.Current.Create<SbControlTemplate>()
                            .GetTemplate<SbForm>();

            return SbControl.TemplateRenderer(template, new Dictionary<string, object>()
            {
                { "Html", html },
                { "Script", script }
            });
        }

        private string RenderScripts()
        {            
            var script = "";
            foreach (var s in Scripts)
            {
                script = s.Script + "\r\n\r\n";
            }

            return script;
        }
    }
}