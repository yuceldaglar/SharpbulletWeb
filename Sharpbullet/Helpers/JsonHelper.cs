using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpBullet.Helpers
{
    public class JsonHelper
    {
        /// <summary>
        /// Entityleri serialize etmek için özel bir metod
        /// Özellikler kompleks entitylerdeki, iç içe çok sayıda entity olursa gereksiz büyük json oluşmasını engeller.
        /// Yine iç içe geçmiş entitylerdeki json'a dönüşme hatalarını çözmek için tasarlandı.
        /// </summary>
        public static string _Serialize(object value)
        {
            return (new JsonHelper()).Serialize(value);
        }

        /// <summary>
        /// Entityleri serialize etmek için özel bir metod
        /// Özellikler kompleks entitylerdeki, iç içe çok sayıda entity olursa gereksiz büyük json oluşmasını engeller.
        /// Yine iç içe geçmiş entitylerdeki json'a dönüşme hatalarını çözmek için tasarlandı.
        /// </summary>
        public string Serialize(object value)
        {
            return Serialize(value, 1);
        }

        private string Serialize(object value, int level)
        {
            if (value == null) return "null";
                        
            if ((value as IList) != null)
            {
                // 2. seviyedeki collectionlar, birer birinci seviye object olarak serialize olmalı
                return SerializeArray(value as IList, level == 2 ? 1 : level);
            }
            /*else if ((asDict = value as IDictionary) != null)
            {
                SerializeObject(asDict);
            }*/
            else
            {
                return SerializeObject(value, level);
            }
        }

        private bool IsDisplayProperty(PropertyInfo property)
        {
            var attrs = property.GetCustomAttributes(typeof(JsonDisplayAttribute), true);

            return attrs != null && attrs.Length > 0;
        }

        private bool IsIgnore(PropertyInfo property)
        {
            var attrs = property.GetCustomAttributes(typeof(JsonIgnoreAttribute), true);

            return attrs != null && attrs.Length > 0;
        }

        private string SerializeArray(IList value, int level)
        {
            var result = "";
            foreach (var item in value)
            {
                if (!string.IsNullOrEmpty(result)) result += ",";
                result += Serialize(item);
            }
            return "[" + result + "]";
        }

        private string SerializeObject(object value, int level)
        {
            var result = "";
            foreach (PropertyInfo property in value.GetType().GetProperties())
            {
                if ((level > 1 && !IsDisplayProperty(property))
                    || IsIgnore(property))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(result)) result += ",";
                try
                {
                    result += "\"" + property.Name + "\": " + SerializeValue(property, property.GetValue(value, null), level);
                }
                catch (Exception ne)
                {
                    /* bu tip propertyleri geç... */
                }
            }
            result = "{" + result + "}";

            return result;
        }

        private string SerializeValue(PropertyInfo property, object value, int level)
        {
            var t = property.PropertyType;
            if (t == typeof(byte) || t == typeof(int) || t == typeof(short) || t == typeof(long))
            {
                return value.ToString();
            }
            else if (t == typeof(bool))
            {
                return ((bool)value) ? "true" : "false";
            }
            else if (t == typeof(DateTime))
            {
                TimeZone localZone = TimeZone.CurrentTimeZone;
                TimeSpan span = localZone.GetUtcOffset(DateTime.Now);
                var timeZone = span.Hours < 0 ? "-" : "+";
                timeZone += span.Hours.ToString().PadLeft(2, '0')+ span.Minutes.ToString().PadLeft(2, '0');

                return "\"" + ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss"+timeZone) + "\"";
            }
            else if (t == typeof(decimal))
            {
                var d = ((decimal)value).ToString(CultureInfo.InvariantCulture);
                if (d.Contains("."))
                {
                    d = d.TrimEnd('0');
                    if (d.EndsWith(".")) d += "0";
                }
                return d;
            }
            else if (t == typeof(double))
            {
                return ((double)value).ToString(CultureInfo.InvariantCulture);
            }
            else if (t == typeof(float))
            {
                return ((float)value).ToString(CultureInfo.InvariantCulture);
            }
            else if (t.IsEnum)
            {
                return "\"" + value + "\"";
            }
            else if (t == typeof(string) || t == typeof(char))
            {
                return "\"" + (value ?? "").ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\\r").Replace("\n", "\\r") + "\"";
            }
            else
            {
                return Serialize(value, level + 1);
            }
        }
    }

    [System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class JsonDisplayAttribute : Attribute
    {
        public JsonDisplayAttribute()
        {            
        }
    }
}