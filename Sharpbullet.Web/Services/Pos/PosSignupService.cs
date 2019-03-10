//using SharpBullet.Web.Entities.Cms;
//using SharpBullet.Web.Entities.Core;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;
//using SharpBullet.OAL;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Security;
//using SharpBullet.Web.Entities.Pos;

//namespace SharpBullet.Web.Services
//{
//    class PosSignupService : BaseService
//    {    
//        [Anonymous]
//        public static PosSignupResult Save(PosSignup formData)
//        {
//            var text = PosText.Instance;

//            Required(formData.Email, text.RequiredError(text.Email()));
//            IsEmail(formData.Email, text.EmailError(text.Email()));
//            Required(formData.Password, text.RequiredError(text.Password()));
//            Required(formData.Password2, text.RequiredError(text.Password2()));
//            IsValid(formData.Password == formData.Password2, text.PasswordsNotMatch());

//            IsUnique<CoreUser>("Email", formData.Email, text.EmailInUseError());

//            var result = new PosSignupResult();
//            Transaction.Instance.Join(() =>
//            {
//                var user = new CoreUser()
//                {
//                    Email = formData.Email,
//                    Password = SecurityHelper.GetMd5Hash(formData.Password)
//                };
//                user.Save();

//                var company = new PosCompany();
//                company.Owner.Id = user.Id;
//                company.Save();

//                var location = new PosCompanyLocation();
//                location.TenantId = company.Id;
//                location.Save();

//                // company owner can change, so make user also a member of company
//                var member = new PosCompanyMember();
//                member.TenantId = company.Id;
//                member.Save();

//                var device = new PosDevice();
//                device.TenantId = company.Id;
//                device.CompanyLocation.Id = location.Id;
//                device.Save();

//                result.Id = company.Id;
//            });

//            return result;
//        }
//    }

//    class PosSignup
//    {
//        public string Email { get; set; }

//        public string Password { get; set; }

//        public string Password2 { get; set; }
//    }

//    class PosSignupResult
//    {
//        public int Id { get; set; }
//    }
//}