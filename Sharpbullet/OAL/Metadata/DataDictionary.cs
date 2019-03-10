using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using SharpBullet.Helpers;

namespace SharpBullet.OAL.Metadata
{
    public class DataDictionary
    {
        #region Singleton
        private DataDictionary()
        {
        }

        private static DataDictionary instance;

        public static DataDictionary Instance
        {
            get
            {
                if (instance == null) instance = new DataDictionary();
                return instance;
            }
            set { instance = value; }
        }
        #endregion

        //private Hashtable entityTypeHash = new Hashtable();

        /* void AddEntityType(string entityName, Type entityType)
        {
            entityTypeHash[entityName] = entityType;
        }*/

        /*public void AddEntities(Type[] entities)
        {
            foreach (Type entityType in entities)
            {
                if (!entityType.IsClass
                    || entityType.IsGenericType
                    || entityType.Name.Contains("<")) continue;

                string className = entityType.Name;
                AddEntityType(className, entityType);
            }
        }*/

        //public int EntityCount { get { return entityTypeHash.Count; } }

        /*public Type GetTypeofEntity(int index)
        {
            int i = 0;
            foreach (object key in entityTypeHash.Keys)
            {
                if (index == i)
                    return (Type)entityTypeHash[key];
                i++;
            }

            throw new IndexOutOfRangeException();
        }*/

        /*public Type GetTypeofEntity(string entityName)
        {
            return (Type)entityTypeHash[entityName];
        }*/

        public EntityDefinition GetEntityDefinition(Type entityType)
        {
            EntityDefinition definition = new EntityDefinition();

            EntityDefinitionAttribute attr = ReflectionHelper.GetAttribute<EntityDefinitionAttribute>(entityType);
            if (attr != null)
            {
                definition.Name = entityType.Name;
                definition.IdMethod = attr.IdMethod;
                definition.StringField = attr.StringField;
                definition.OptimisticLockField = attr.OptimisticLockField;
            }
            else
                throw new Exception("Entity definition is missing: " + entityType.Name);

            return definition;
        }

        /// <summary>
        /// Finds the first field that matches the given fieldType. If not found, returns "Id".
        /// </summary>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="fieldType">Type of the field</param>
        /// <returns></returns>
        public string GetFirstFieldName(Type entityType, Type fieldType)
        {
            EntityDefinition entityDefinition = GetEntityDefinition(entityType);
            string strProp = entityDefinition.StringField;
            if (String.IsNullOrEmpty(strProp))
            {
                PropertyInfo firstStrProp = ReflectionHelper.GetFirstProperty(entityType, fieldType);
                strProp = firstStrProp == null ? "Id" : firstStrProp.Name;
            }
            return strProp;
        }

        public List<FieldDefinitionAttribute> GetAllFields(Type entityType)
        {
            PropertyInfo[] props = entityType.GetProperties();

            List<FieldDefinitionAttribute> result = new List<FieldDefinitionAttribute>();
            foreach (PropertyInfo property in props)
            {
                result.Add(
                    GenerateFieldDefinition(entityType, property));
            }
            return result;
        }

        public FieldDefinitionAttribute GetFieldDefinition(Type entityType, PropertyInfo property)
        {
            return GenerateFieldDefinition(entityType, property);
        }

        private FieldDefinitionAttribute GenerateFieldDefinition(Type entityType, PropertyInfo property)
        {
            FieldDefinitionAttribute attr = null;
                       
            if(property.Name != "Id")
                attr = ReflectionHelper.GetAttribute<FieldDefinitionAttribute>(property);
            else
                attr = ReflectionHelper.GetAttribute<FieldDefinitionAttribute>(entityType);

            if (attr != null)
            {
                attr.InstanceProperty = property;
            }
            else
            {
                attr = new FieldDefinitionAttribute(property);
            }

            return attr;
        }

        public List<FieldDefinitionAttribute> GetFilteringFields(Type entityType)
        {
            EntityDefinition definition = new EntityDefinition();

            List<FieldDefinitionAttribute> list = new List<FieldDefinitionAttribute>();
            PropertyInfo[] props = entityType.GetProperties();
            foreach (PropertyInfo pi in props)
            {
                FieldDefinitionAttribute f = GenerateFieldDefinition(entityType, pi);
                if (f.IsFiltered)
                    list.Add(f);
            }

            return list;
        }
    }
}
