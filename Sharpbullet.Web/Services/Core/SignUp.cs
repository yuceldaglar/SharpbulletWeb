using SharpBullet.Helpers;
using SharpBullet.OAL;
using SharpBullet.Web.Entities.Core;
using SharpBullet.Web.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SharpBullet.Web.Services.Core
{
    public class SignUp : BaseService
    {
        public static SbResult Create(SbUser user)
        {
            var text = SbText.Instance;

            Required(user.Email, text.RequiredError(text.Email()));
            IsEmail(user.Email, text.EmailError(text.Email()));
            Required(user.Password, text.RequiredError(text.Password()));
            Required(user.Password2, text.RequiredError(text.Password()));

            var withSalt = user.Password + SbApplication.Current.Access.Configuration.Salt;
            var passwordHash = SecurityHelper.GetMd5Hash(user.Password);
            user.Password = passwordHash;

            SbUser u = Transaction.Instance.Where<SbUser>("Email=@prm0", user.Email);          
            if (u != null && u.Exist())
                throw new Exception(SbText.Instance.SignUpError());

            user.Save();

            return SbResult.GetSuccess();
        }
    }
}