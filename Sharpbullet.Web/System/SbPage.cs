using System;
using System.Text;
using System.Web;
using System.Web.UI;

namespace SharpBullet.Web
{
    public class SbPage : Page
    {
        /// <summary>
        /// Sayfa herkese açık olsun isteniyorsa kullanılabilir. Üye olmayanlarda görebilir.
        /// </summary>
        public bool PublicPage { get; set; }

        /// <summary>
        /// Tüm üyelerin sayfayı görmesi için * kullanılmalı veya boş bırakılmalıdır.
        /// </summary>
        public string Roles { get; set; }

        protected override void OnLoad(EventArgs e)
        {
            if (!IsAuthorized())
            {
                Response.Redirect(SbApp.Access.Configuration.LoginPage, true);
            }
            base.OnLoad(e);
        }

        public virtual bool IsAuthorized()
        {
            // Yetkilendirme yok, sayfa herkese açık demektir
            if (PublicPage)
            {
                return true;
            }

            var user = SbApplication.Current.Access.GetUser();

            var canSeeEveryPage = user.IsAdmin;
            if (canSeeEveryPage)
            {
                return true;
            }

            // Tüm üyelere açık sayfa anlamına gelir
            if (Roles == "*" || string.IsNullOrEmpty(Roles))
            {
                return SbApplication.Current.Access.GetUser().Exist();
            }

            // Kullanıcının rolü ile sayfanın rolleri arasında eşleşen var mı
            /*string[] roles = this.Roles.Split(',');
            foreach (var role in roles)
            {
                if (role == user.Role)
                {
                    return true;
                }
            }
            string roleName = SharpBullet.OAL.Transaction.Instance.ExecuteScalarS("select RoleName from Role where Id=@prm0", user.RoleId);
            if (roleName == "Genel")
                return true;*/
            return false;
        }

        public string Api(string methodName)
        {
            var s = this.AppRelativeVirtualPath;
            s = s.Substring(s.LastIndexOf('/') + 1);
            if (s.EndsWith(".aspx")) s = s.Substring(0, s.Length - 5);

            if (s.Contains("_"))
            {
                s = s.Substring(s.LastIndexOf('_') + 1);
            }

            return s + "." + methodName;
        }
    }
}
