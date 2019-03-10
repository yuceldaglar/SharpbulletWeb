using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace SharpBullet.Helpers
{
    public class EnumHelper
    {
        public static string GetSqlSwitch<T>(string column)
        {
            return GetSqlSwitch<T>(column, null);
        }

        public static string GetSqlSwitch<T>(string column, Dictionary<string, string> labelDictionary)
        {
            return GetSqlSwitch(typeof(T), column, labelDictionary);
        }

        public static string GetSqlSwitch(Type enumType, string column)
        {
            return GetSqlSwitch(enumType, column, null);
        }

        public static string GetSqlSwitch(Type enumType, string column, Dictionary<string, string> labelDictionary)
        {
            string result = string.Format("CASE IsNull({0}, 0) \r\n", column);                        
            foreach(object value in Enum.GetValues(enumType))
            {
                int i = (int)value;
                string s = value.ToString();

                if (labelDictionary != null && labelDictionary.ContainsKey(s))
                    s = labelDictionary[s];
                
                result += string.Format("WHEN {0} THEN '{1}' \r\n", i, s);
            }
            result += "END";

            return result;
        }
    }
}
