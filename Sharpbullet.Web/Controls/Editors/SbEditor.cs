using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web.Controls.Editors
{
    public class SbEditor : SbControl
    {
        public string Text { get; set; }
        public string DataSource { get; set; }
        public string DataField { get; set; }

        protected override Dictionary<string, object> GetValues()
        {
            var values = base.GetValues();
            values["Text"] = Text;
            values["DataSource"] = DataSource;
            values["DataField"] = DataField;

            return values;
        }
    }
}