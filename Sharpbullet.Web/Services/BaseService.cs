using SharpBullet.OAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web.Services
{
    public class BaseService
    {
        /// <summary>
        /// call Required before IsEmail
        /// </summary>
        public static void IsEmail(string email, string message)
        {
            // -@-.-
            var i = email.IndexOf('@');
            var j = i > -1 ? email.IndexOf('.', i) : -1;

            if (i > 0
                && j > (i + 1)
                && j < (email.Length - 1)
                ) return;

            throw new ApplicationException(message);
        }

        public static void IsValid(bool isValid, string message)
        {
            if (isValid) return;

            throw new ApplicationException(message);
        }

        public static void Required(string value, string message)
        {
            if (value != null && !string.IsNullOrEmpty(value.Trim())) return;

            throw new ApplicationException(message);
        }

        public static void IsUnique<T>(string fieldName, string value, string message)
        {
            var sql = string.Format("select count(*) from {0} where {1}=@prm0",
                                    typeof(T).Name, fieldName);

            var i = Transaction.Instance.ExecuteScalarI(sql, value);
            if (i > 0) throw new ApplicationException(message);
        }
    }

    [global::System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class AnonymousAttribute : Attribute
    {
        public AnonymousAttribute()
        {
        }
    }
}