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
    public class Login : BaseService
    {
        public static SbResult Create(SbUser user)
        {
            var text = SbText.Instance;

            Required(user.Email, text.RequiredError(text.Email()));
            IsEmail(user.Email, text.EmailError(text.Email()));
            Required(user.Password, text.RequiredError(text.Password()));

            var withSalt = user.Password + SbApplication.Current.Access.Configuration.Salt;
            var password = SecurityHelper.GetMd5Hash(user.Password);
            user.Password = ""; // removed for security purposes

            SbUser u = Transaction.Instance.Where<SbUser>(
                            "Email=@prm0 and Password=@prm1",
                            user.Email, password);
            u.Password = ""; // removed for security purposes

            if (u == null || !u.Exist())
                throw new Exception(SbText.Instance.LoginError());                
            
            SbApp.Access.SetUser(u);

            return SbResult.GetSuccess(SbApp.Access.Configuration.MemberHomePage);
        }

        [Anonymous]
        public static SbResult Delete()
        {
            SbApplication.Current.Access.SetUser(null);

            return SbResult.GetSuccess();
        }
    }
}