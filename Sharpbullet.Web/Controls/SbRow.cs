using SharpBullet.Web.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web.Controls
{
    public class SbRow : SbContainer
    {
        protected override Dictionary<string, object> GetValues()
        {
            return new Dictionary<string, object>()
            {
                { "Class", Class }
            };
        }
    }
}
