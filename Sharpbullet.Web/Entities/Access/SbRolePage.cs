using SharpBullet.OAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBullet.Web.Entities.Core
{
    public class SbRolePage : BaseEntityTenant
    {
        public SbRole Role { get; set; }

        public string Page { get; set; }

        public bool FullControl { get; set; }
        public bool CanRead { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }




        public SbRolePage()
        {
            Role = new SbRole();
        }
    }
}