using SharpBullet.OAL;
using SharpBullet.OAL.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBullet.Web.Entities.Core
{
    public class SbRole : BaseEntityTenant
    {
        [FieldDefinition(UniqueIndexGroup="IX_Role_RoleName")]
        public string RoleName { get; set; }
    }
}
