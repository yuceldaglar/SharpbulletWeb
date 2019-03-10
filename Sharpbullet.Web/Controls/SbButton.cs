using SharpBullet.Web.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web.Controls
{
    public class SbButton : SbControl
    {
        public string Text { get; set; }
        public string Click { get; set; }

        protected override Dictionary<string, object> GetValues()
        {
            var values = base.GetValues();
            values["Text"] = Text;
            values["Click"] = Click;

            return values;
        }
    }
}