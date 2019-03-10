using SharpBullet.Web.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web
{ 
    interface ICoreText
    {
        string TenantParameterMissing();
        string Email();
        string Password();
        string Password2();
        string LoginError();
        string SignUpError();
        string RequiredError(string fieldName);
        string EmailError(string fieldName);

        string MissingService(string serviceName);
        string MissingMethod(string methodName);
        string OnlyOneParameter(string methodName);
    }

    class SbText : ICoreText
    {
        private static SbText instance;

        public static CoreLanguage Language
        {
            get
            {
                return CoreLanguage.TR;
            }
        }

        public static ICoreText Instance
        {
            get
            {
                switch (Language)
                {
                    case CoreLanguage.TR:
                    default:
                        if (instance == null) instance = new SbText();
                        return instance;
                    case CoreLanguage.EN:
                        return SbTextEN.Instance;
                }
            }
        }

        public string Email() { return "E-posta"; }
        public string Password() { return "Şifre"; }
        public string Password2() { return "Şifre (tekrar)"; }
        public string LoginError() { return "Kullanıcı adı veya şifre hatalı."; }
        public string SignUpError() { return "Mail adresi daha önce kullanılmış."; }
        public string TenantParameterMissing() { return "Sistem hatası: tenant parametresi eksik."; }
        public string RequiredError(string fieldName) { return string.Format("Lütfen bu alana bilgi giriniz: '{0}'", fieldName); }
        public string EmailError(string fieldName) { return string.Format("Geçerli bir eposta girmelisiniz: '{0}'", fieldName); }
        public string MissingService(string serviceName) { return string.Format("Servis bulunamadı: {0}", serviceName); }
        public string MissingMethod(string methodName) { return string.Format("Metod bulunamadı: {0}", methodName); }
        public string OnlyOneParameter(string methodName) { return string.Format("Sadece bir parametre desteklenmektedir: {0}", methodName); }
    }

    class SbTextEN : ICoreText
    {
        private static SbTextEN instance;

        public static SbTextEN Instance
        {
            get
            {
                if (instance == null) instance = new SbTextEN();
                return instance;
            }
        }

        public string Email() { return "Email"; }
        public string Password() { return "Password"; }
        public string Password2() { return "Password"; }
        public string LoginError() { return "Username or password is incorrect."; }
        public string SignUpError() { return "Email address already in use."; }
        public string TenantParameterMissing() { return "System error: tenant parameter is missing."; }
        public string RequiredError(string fieldName) { return string.Format("Please fill the field: '{0}'", fieldName); }
        public string EmailError(string fieldName) { return string.Format("Email is not valid: '{0}'", fieldName); }
        public string MissingService(string serviceName) { return string.Format("Missing service: {0}", serviceName); }
        public string MissingMethod(string methodName) { return string.Format("Missing method: {0}", methodName); }
        public string OnlyOneParameter(string methodName) { return string.Format("Only one parameter is supported: {0}", methodName); }
    }
}