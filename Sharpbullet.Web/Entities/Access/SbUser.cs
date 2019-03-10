using SharpBullet.OAL;
using SharpBullet.OAL.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBullet.Web.Entities.Core
{
    public class SbUser : BaseEntityDeletable
    {
        [FieldDefinition(IsRequired=true, UniqueIndexGroup="IX_User_Email")]
        public string Email { get; set; }

        [FieldDefinition(IsRequired = true)]
        public string Password { get; set; }

        [NonPersistent]
        public string Password2 { get; set; }

        public string Username { get; set; }

        /// <summary>
        /// System administrator, root  access
        /// </summary>
        public bool IsAdmin { get; set; }

        public int RoleId { get; set; }

        public string ExternalKey { get; set; }

        public bool IsConfirmed { get; set; }

        public string ConfirmationKey { get; set; }
    }   
}