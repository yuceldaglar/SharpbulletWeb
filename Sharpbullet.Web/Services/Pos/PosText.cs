//using SharpBullet.Web.Entities.Core;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace System
//{
//    public interface IPosText 
//    {
//        string SignupFormTitle();
//        string Email();
//        string Password();
//        string Password2();
//        string SignUp();
//        string PasswordsNotMatch();
//        string EmailInUseError();

//        string LoginFormTitle();
//        string Login();

//        string RequiredError(string fieldName);
//        string EmailError(string fieldName);

//        string AddCommand();
//    }

//    public class PosText : IPosText
//    {
//        private static PosText instance;

//        public static CoreLanguage Language
//        {
//            get
//            {
//                return CoreLanguage.TR;
//            }
//        }

//        public static IPosText Instance
//        {
//            get
//            {
//                switch (Language)
//                {
//                    case CoreLanguage.TR:
//                    default:
//                        if (instance == null) instance = new PosText();
//                        return instance;
//                    case CoreLanguage.EN:
//                        return PosTextEN.Instance;
//                }
//            }
//        }

//        public string SignupFormTitle()
//        {
//            return "Ücretsiz Hesap Oluşturun";
//        }

//        public string Email()
//        {
//            return "E-posta";
//        }

//        public string Password()
//        {
//            return "Şifre";
//        }

//        public string Password2()
//        {
//            return "Şifre (tekrar)";
//        }

//        public string PasswordsNotMatch()
//        {
//            return "Girdiğiniz iki şifre farklı olmuş, lütfen tekrar deneyiniz.";
//        }

//        public string RequiredError(string fieldName)
//        {
//            return string.Format("Lütfen bu alana bilgi giriniz: '{0}'", fieldName);
//        }

//        public string EmailError(string fieldName)
//        {
//            return string.Format("Geçerli bir eposta girmelisiniz: '{0}'", fieldName);
//        }

//        public string SignUp() { return "Kayıt Ol"; }

//        public string EmailInUseError()
//        {
//            return "Bu eposta adresi ile zaten üye olunmuş.";
//        }

//        public string LoginFormTitle()
//        {
//            return "Giriş";
//        }

//        public string Login() { return "Giriş"; }

//        public string AddCommand() { return "Ekle"; }
//    }

//    public class PosTextEN : IPosText
//    {
//        private static PosTextEN instance;

//        public static PosTextEN Instance
//        {
//            get
//            {
//                if (instance == null) instance = new PosTextEN();
//                return instance;
//            }
//        }

//        public string SignupFormTitle()
//        {
//            return "Create Your Free Acount";
//        }

//        public string Email()
//        {
//            return "Email";
//        }

//        public string Password()
//        {
//            return "Password";
//        }

//        public string Password2()
//        {
//            return "Password (again)";
//        }

//        public string PasswordsNotMatch()
//        {
//            return "Passwords does not match. Please try again.";
//        }

//        public string RequiredError(string fieldName)
//        {
//            return string.Format("Please fill the field: '{0}'", fieldName);
//        }

//        public string EmailError(string fieldName)
//        {
//            return string.Format("Email is not valid: '{0}'", fieldName);
//        }

//        public string SignUp() { return "Sign Up"; }


//        public string EmailInUseError()
//        {
//            return "The email address you have entered is already registered.";
//        }

//        public string LoginFormTitle()
//        {
//            return "Login";
//        }

//        public string Login() { return "Login"; }

//        public string AddCommand() { return "Add"; }
//    }
//}