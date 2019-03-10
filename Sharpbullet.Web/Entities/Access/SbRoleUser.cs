using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBullet.Web.Entities.Core
{
    public class SbRoleUser : BaseEntityTenant
    {
        public SbRole Role { get; set; }
        public SbUser User { get; set; }




        public SbRoleUser()
        {
            Role = new SbRole();
            User = new SbUser();
        }
    }
}