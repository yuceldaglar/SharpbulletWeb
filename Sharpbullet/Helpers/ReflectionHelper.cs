using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading;
using System.IO;
using SharpBullet.Helpers;

namespace SharpBullet.Helpers
{
    public static class ReflectionHelper
    {
        public static PropertyInfo[] GetPropertiesExceptMarkedWithAttr(Type type, Type attribute)
        {
            List<PropertyInfo> list;
            PropertyInfo[] properties;

            list = new List<PropertyInfo>();
            properties = type.GetProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                if (ArrayHelper.IsNull(properties[i].GetCustomAttributes(attribute, false)))
                {
                    list.Add(properties[i]);
                }
            }

            return list.ToArray();
        }

        public static T GetAttribute<T>(Type type)
        {
            object[] attrs = type.GetCustomAttributes(typeof(T), true);
            if (attrs != null && attrs.Length == 1)
                return (T)attrs[0];

            return default(T);
        }

        public static T GetAttribute<T>(PropertyInfo property)
        {
            object[] attrs = property.GetCustomAttributes(typeof(T), true);
            if (attrs != null && attrs.Length == 1)
                return (T)attrs[0];

            return default(T);
        }

        public static PropertyInfo GetPropertyInfo(Type type, string field)
        {
            if (!field.Contains("."))
                return type.GetProperty(field);
            else
            {
                string[] fieldNames = field.Split('.');
                PropertyInfo pi = null;
                foreach (string fieldName in fieldNames)
                {
                    pi = type.GetProperty(fieldName);
                    type = pi.PropertyType;
                }
                return pi;
            }
        }

        public static PropertyInfo GetFirstProperty(Type type, Type propertyType)
        {
            foreach (PropertyInfo pi in type.GetProperties())
                if (pi.PropertyType == propertyType)
                    return pi;
            return null;
        }

        public static object GetValue(object instance, string field)
        {
            if (!field.Contains("."))
                return instance.GetType().GetProperty(field).GetValue(instance, null);
            else
            {
                string[] fieldNames = field.Split('.');
                PropertyInfo pi = null;
                foreach (string fieldName in fieldNames)
                {
                    pi = instance.GetType().GetProperty(fieldName);
                    instance = pi.GetValue(instance, null);
                }
                return instance;
            }
        }

        private static void SetPropertyValue(PropertyInfo pi, object instance, object value)
        {
            if (pi.PropertyType == typeof(DateTime) && value != null && value.GetType() == typeof(string))
            {
                value = DateTime.Parse((string)value, new System.Globalization.CultureInfo("Tr-tr"));
            }
            else if (pi.PropertyType == typeof(DateTime) && value != null && value.GetType() == typeof(double)) //excel import için yazýldý
            {
                value = DateTime.FromOADate((double)value);
            }
            else if (pi.PropertyType == typeof(bool) && (value.ToString() == "Unchecked" || value.ToString() == "Checked"))
            {
                if (value.ToString() == "Unchecked") value = false; else value = true;
            }
            else if (!pi.PropertyType.IsEnum)
            {
                value = Convert.ChangeType(value, pi.PropertyType, Thread.CurrentThread.CurrentCulture);
            }
            else
            {
                value = Enum.Parse(pi.PropertyType, value.ToString());
            }

            pi.SetValue(instance, value, null);
        }

        public static void SetValue(object instance, string field, object value)
        {
            if (!field.Contains("."))
            {
                PropertyInfo pi = instance.GetType().GetProperty(field);

                //Bir valuetype için deðer gelmemiþse set etmeye gerek olmamalý. 
                if ((value == null || (string.IsNullOrEmpty(value.ToString())))
                    && pi.PropertyType.IsValueType)
                {
                    return;
                }

                SetPropertyValue(pi, instance, value);
                /*
                if (pi.PropertyType == typeof(DateTime) && value != null && value.GetType() == typeof(string))
                    value = DateTime.Parse((string)value, new System.Globalization.CultureInfo("Tr-tr"));
                else if (pi.PropertyType == typeof(DateTime) && value != null && value.GetType() == typeof(double)) //excel import için yazýldý
                    value = DateTime.FromOADate((double)value);
                else if (pi.PropertyType == typeof(bool) && (value.ToString() == "Unchecked" || value.ToString() == "Checked"))
                {
                    if (value.ToString() == "Unchecked") value = false; else value = true;
                }
                else if (!pi.PropertyType.IsEnum)
                    value = Convert.ChangeType(value, pi.PropertyType, Thread.CurrentThread.CurrentCulture);
                else
                    value = Enum.Parse(pi.PropertyType, value.ToString());

                pi.SetValue(instance, value, null);
                */
                //--instance.GetType().GetProperty(field).SetValue(instance, value, null);
            }
            else
            {
                string[] fieldNames = field.Split('.');
                PropertyInfo pi = null;
                string fieldName = null;
                for (int i = 0; i < fieldNames.Length; i++)
                {
                    fieldName = fieldNames[i];
                    pi = instance.GetType().GetProperty(fieldName);
                    if (pi == null) return;

                    if (i < (fieldNames.Length - 1))
                    {
                        object temValue = pi.GetValue(instance, null);
                        if (temValue == null)
                        {
                            temValue = Activator.CreateInstance(pi.PropertyType);
                            pi.SetValue(instance, temValue, null);
                        }
                        instance = temValue;
                    }
                }

                //Bir valuetype için deðer gelmemiþse set etmeye gerek olmamalý. 
                if (pi.PropertyType.IsValueType &&
                    (value == null || string.IsNullOrEmpty(value.ToString()))) return;

                SetPropertyValue(pi, instance, value);
                /*
                if (pi.PropertyType == typeof(DateTime) && value != null && value.GetType() == typeof(string))
                    value = DateTime.Parse((string)value, new System.Globalization.CultureInfo("Tr-tr"));
                else if (pi.PropertyType == typeof(DateTime) && value != null && value.GetType() == typeof(double)) //excel import için yazýldý
                    value = DateTime.FromOADate((double)value);
                else if (!pi.PropertyType.IsEnum)
                    value = Convert.ChangeType(value, pi.PropertyType);
                else
                    value = Enum.Parse(pi.PropertyType, value.ToString());

                pi.SetValue(instance, value, null);
                */
            }
        }

        public static Type GetDotNetType(string typeName)
        {
            switch (typeName)
            {
                case "string":
                case "String": return typeof(string);
                case "int": return typeof(int);
                case "DateTime": return typeof(DateTime);
                case "decimal": return typeof(decimal);
                case "bool": return typeof(bool);
                default:
                    Type type = Type.GetType(typeName);
                    if (type == null)
                        throw new ArgumentException(typeName + " .Net karþýlýðý bulunamadý!Reflection Helperda");
                    return type;
            }
        }

        public static string Serialize(object obj)
        {

            System.Xml.Serialization.XmlSerializer xml2 = new System.Xml.Serialization.XmlSerializer(obj.GetType());
            StringBuilder b = new StringBuilder();
            StringWriter wr2 = new StringWriter(b);
            xml2.Serialize(wr2, obj);
            string ser = b.ToString();
            return ser;
        }

        public static T Deserialize<T>(string str)
        {
            System.Xml.Serialization.XmlSerializer xml2 = new System.Xml.Serialization.XmlSerializer(typeof(T));
            StringReader wr2 = new StringReader(str);
            return (T)xml2.Deserialize(wr2);
        }
    }
}
