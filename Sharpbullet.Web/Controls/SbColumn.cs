using SharpBullet.Web.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web.Controls
{
    public class SbColumn : SbContainer
    {
        public int ColSize { get; set; }

        public SbColumn()
        {
            ColSize = 12;
        }

        protected override Dictionary<string, object> GetValues()
        {
            var values = base.GetValues();
            var colTemplate = SbApplication.Current
                .Create<SbControlTemplate>()
                .GetTemplate("SbColumn_ColSize");

            var colSizeStr = TemplateRenderer(colTemplate, new Dictionary<string, object>()
            {
                { "ColSize", ColSize }
            });

            return new Dictionary<string, object>()
            {
                { "Class", Class },
                { "ColSize", colSizeStr }
            };       
        }
    }
}