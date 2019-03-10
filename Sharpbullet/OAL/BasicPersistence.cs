using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using SharpBullet.OAL.Metadata;
using System.ComponentModel;
using SharpBullet.Helpers;

namespace SharpBullet.OAL
{
    public class BasicPersistence : PersistenceStrategy
    {
        public override string GetTableNameOf(Type type)
        {
            return type.Name;
        }

        public override string GetSelectStatementFor(Type type, string[] filterFields, string[] filterValues)
        {
            return null;
        }

        public override string GetKeyColumnOf(Type type)
        {
            return "Id";
        }

        //Support foreign key mapping, written like "Somefield_Id"
        public override void Fill(object entity, System.Data.DataRow dataRow)
        {
            string fieldName;
            object value;
            Type type;
            PropertyInfo property;

            type = entity.GetType();
            for (int i = 0; i < dataRow.ItemArray.Length; i++)
            {
                fieldName = dataRow.Table.Columns[i].ColumnName;
                value = dataRow.ItemArray[i];

                //DBNull deðerinin c# ta karþýlýðý olmadýðýna göre set etmeden býrakacaz, default deðer kalacak
                if (value == DBNull.Value) continue;

                property = type.GetProperty(fieldName); // buraya taþýndý, böylece içinde _ karakteri olan alanlarda desteklenecek.

                if (property == null && fieldName.Contains("_"))
                {
                    //Joinden gelen alanlarý destekler, aslýnda yukardaki id alanýný da kapsayabilir
                    // ama çok sonradan eklendiði için ayrý yazýldý.
                    ReflectionHelper.SetValue(entity, fieldName.Replace('_', '.'), value);
                }
                else
                {
                    if (property != null)
                    {
                        if (System.DBNull.Value.Equals(value))
                        {
                            if (property.PropertyType.Equals(typeof(string)))
                                value = "";
                            else if (property.PropertyType.Equals(typeof(long)))
                                value = (long)0;
                            else if (property.PropertyType.Equals(typeof(int)))
                                value = (int)0;
                            else if (property.PropertyType.Equals(typeof(bool)))
                                value = false;
                            else if (property.PropertyType.Equals(typeof(decimal)))
                                value = decimal.Zero;
                            else if (property.PropertyType.Equals(typeof(byte)))
                                value = (byte)0;
                            else if (property.PropertyType.Equals(typeof(DateTime)))
                                value = DateTime.MinValue;
                            else
                                value = null; //c# ta null böyle olmalý :)
                        }

                        //Bu durumda default deðer kalmýþ olacak. Yapacak biþey yok.
                        if (value == null && property.PropertyType.IsValueType) continue;

                        if (property.PropertyType.IsEnum)
                        {
                            //--Array a = Enum.GetValues(property.PropertyType);
                            //--foreach (object o in a) if (Convert.ToInt32(o) == (Int32)value) value = o;
                            //----value = Enum.Parse(property.PropertyType, (string)value);
                            value = Convert.ChangeType(value, typeof(int));
                        }
                        else
                        {
                            // remote datatable json dönüþümünde byte array'ler problemini düzeltmek için
                            if (value != null
                                && property.PropertyType == typeof(byte[])
                                && value.GetType() == typeof(string))
                            {
                                value = Convert.FromBase64String(value.ToString());
                            }
                            //Sebebi bazý uyumsuzluklar: örneðin mysql db de bit, okuyunca long geliyor, oysa bool olmasý lazým.
                            //ilk null kontrolü stringler için
                            else if (value != null
                                && property.PropertyType != typeof(Object)
                                && property.PropertyType != value.GetType())
                            {
                                value = Convert.ChangeType(value, property.PropertyType);
                            }
                        }
                        property.SetValue(entity, value, null);
                    }
                }
            }
        }

        public override bool CanPersist(Type type)
        {
            return true; //We have no idea if we can persist?
        }

        public override string[] GetPersistentFieldNamesOf(Type type)
        {
            PropertyInfo[] properties;
            string[] fieldNames;

            properties = GetPersistentProperties(type);
            fieldNames = Array.ConvertAll<PropertyInfo, string>(properties, delegate(PropertyInfo p)
            {
                return MapPropertyNameToFieldName(p);
            });

            return fieldNames;
        }

        public override PropertyInfo[] GetPersistentProperties(Type type)
        {
            PropertyInfo[] properties;

            properties = ReflectionHelper.GetPropertiesExceptMarkedWithAttr(type, typeof(NonPersistentAttribute));
            return properties;
        }

        public override object[] GetFieldValuesOf(object entity, string[] fieldNames)
        {
            Type type;
            PropertyInfo propertyInfo;
            object[] result;
            object key;
            string propertyName;

            type = entity.GetType();
            result = new object[fieldNames.Length];
            for (int i = 0; i < fieldNames.Length; i++)
            {
                propertyName = MapFieldNameToPropertyName(fieldNames[i]);

                propertyInfo = type.GetProperty(propertyName);

                // enum deðerleri int olarak kaydediyoruz
                object value = propertyInfo.GetValue(entity, null);
                if (propertyInfo.PropertyType.IsEnum) value = (int)value;
                result[i] = value;

                if (IsForeignKey(fieldNames[i]))
                {
                    key = DBNull.Value;
                    if (result[i] != null)
                    {
                        propertyInfo = result[i].GetType().GetProperty("Id");
                        int tempKey = (Int32)propertyInfo.GetValue(result[i], null);
                        if (tempKey > 0)
                            key = tempKey;
                    }
                    result[i] = key;
                }
            }


            return result;
        }

        public override string[] GetInsertFieldNamesOf(Type type)
        {
            string[] fieldNames;

            fieldNames = GetPersistentFieldNamesOf(type);

            if (GetIdMethodFor(type) != IdMethod.UserSubmitted)
                fieldNames = StrHelper.Remove("Id", fieldNames);

            return fieldNames;
        }

        public override string[] GetUpdateFieldNamesOf(Type type)
        {
            string[] fieldNames;
            string optimisticLockField = null;

            fieldNames = GetPersistentFieldNamesOf(type);
            fieldNames = StrHelper.Remove("Id", fieldNames);

            optimisticLockField = GetOptimisticLockField(type);
            fieldNames = StrHelper.Remove(optimisticLockField, fieldNames);

            return fieldNames;
        }

        public override string[] GetSelectFieldNamesOf(Type type)
        {
            string[] fieldNames;

            fieldNames = GetPersistentFieldNamesOf(type);

            return fieldNames;
        }

        public override object GetKeyValueOf(object entity)
        {
            PropertyInfo keyProperty;
            object value;

            keyProperty = entity.GetType().GetProperty("Id");
            value = keyProperty.GetValue(entity, null);

            return value;
        }

        public override IdMethod GetIdMethodFor(Type type)
        {
            EntityDefinition definition = DataDictionary.Instance.GetEntityDefinition(type);
            if (definition == null)
                return IdMethod.Identity;
            else
                return definition.IdMethod;
        }

        public override string GetIdSqlFor(Type type)
        {
            //Do not implement this method. Because Basic strategy is based on IdMethod.Identity.
            throw new Exception("The method or operation is not implemented.");
        }

        public override object GetIdFor(object entity, Transaction transaction)
        {
            //Do not implement this method. Because Basic strategy is based on IdMethod.Identity.
            throw new Exception("The method or operation is not implemented.");
        }

        private string MapPropertyNameToFieldName(PropertyInfo propertyInfo)
        {
            string fieldName;

            fieldName = propertyInfo.Name;

            if (propertyInfo.Name != "Id"
                && propertyInfo.PropertyType.IsClass
                && !propertyInfo.PropertyType.Equals(typeof(string))
                && !propertyInfo.PropertyType.Equals(typeof(byte[])))
            {
                fieldName += "_Id";
            }

            return fieldName;
        }

        private string MapFieldNameToPropertyName(string fieldName)
        {
            string propertyName;

            propertyName = fieldName;
            if (propertyName.EndsWith("_Id"))
                propertyName = propertyName.Substring(0, propertyName.Length - 3);

            return propertyName;
        }

        private bool IsForeignKey(string fieldName)
        {
            return fieldName.EndsWith("_Id");
        }

        public override string GetOptimisticLockField(Type type)
        {
            EntityDefinition definition = DataDictionary.Instance.GetEntityDefinition(type);
            if (definition == null)
                return "";
            else
                return definition.OptimisticLockField;
        }

        public override object GetOptimisticLockValue(object entity)
        {
            string fieldName = GetOptimisticLockField(entity.GetType());
            PropertyInfo info = entity.GetType().GetProperty(fieldName);
            return info.GetValue(entity, null);
        }

        public override string MapProperty(PropertyInfo property)
        {
            if (property.PropertyType.IsEnum)
            {
                return typeof(int).Name;
            }

            return property.PropertyType.Name;
        }
    }
}