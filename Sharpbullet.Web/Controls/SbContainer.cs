using SharpBullet.Web.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web.Controls
{
    public class SbContainer : SbControl
    {
        private List<SbControl> controls;

        public List<SbControl> Controls
        {
            get
            {
                if (controls == null) controls = new List<SbControl>();
                return controls;
            }
            set { controls = value; }
        }

        public SbContainer Add(params SbControl[] control)
        {
            Controls.AddRange(control);
            return this;
        }

        public override string Render(SbApplication app)
        {
            var template = app.Create<SbControlTemplate>();
            var values = GetValues();
            values["Content"] = RenderControls(app);

            return TemplateRenderer(
                template.GetTemplate(this.GetType().Name),
                values
            );
        }          

        protected virtual string RenderControls(SbApplication app)
        {
            var result = "";
            if (Controls == null || Controls.Count == 0) return result;

            foreach (var control in Controls)
            {
                result += control.Render(app) + "\r\n";
            }
            return result;
        }
    }
}
