using System;
using System.Collections.Generic;
using System.Text;
using SharpBullet.OAL;
using SharpBullet.OAL.Metadata;
using System.Reflection;
using System.ComponentModel;
using System.Data;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using SharpBullet.Helpers;

namespace SharpBullet.Entities
{
    public class SharpPointer<T> where T: new()
    {
        private int id = 0;

        public int Id {
            get {
                if (value!=null)
                {
                    return (value as SbEntity).Id;
                }
                return id;
            }
            set {
                if (this.value != null)
                {
                    (this.value as SbEntity).Id = value;
                }
                else
                {
                    id = value;
                }
            }
        }
        
        private T value;

        [JsonIgnoreAttribute]
        public T Value
        {
            get
            {
                if (value == null)
                {
                    value = new T();
                    (value as SbEntity).Id = this.id;
                }
                return value;
            }
            set { this.value = value; }
        }

        [JsonIgnoreAttribute]
        public Type PointerType { get { return typeof(T); } }




        public virtual bool Exist()
        {
            return Id != 0;
        }

        public virtual bool NotExist()
        {
            return !Exist();
        }
    }

    [FieldDefinition(TypeName = "Int32", IsFiltered = false)] //For 'Id' 
    [EntityDefinition(IdMethod = IdMethod.Identity, OptimisticLockField="RowVersion")]
    public class SbEntity
    {
        protected bool isRead = false;

        private Int32 id;

        [JsonDisplay]
        public Int32 Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                isRead = false;
            }
        }

        private byte rowVersion;
        public byte RowVersion
        {
            get { return rowVersion; }
            set { rowVersion = value; }
        }

        public virtual void Validate()
        {
            //Loop each field
            //  if field has definition and validations in it
            //  then call validate method
            Type type = GetType();
            PropertyInfo[] properties = PersistenceStrategyProvider.FindStrategyFor(type).GetPersistentProperties(type);

            if (properties != null)
            {
                foreach (PropertyInfo property in properties)
                {
                    FieldDefinitionAttribute definition = DataDictionary.Instance.GetFieldDefinition(type, property);
                    if(definition==null)
                        throw new Exception("Definition okunamadý: " + property.Name);
                    object value = GetValue(property.Name);

                    //Length validation for string fields
                    if (definition.TypeName == typeof(string).Name)
                    {
                        if (definition.Length > 0 // sýfýr ise sýnýrsýz yazýlabilir
                            && value != null && definition.Length < value.ToString().Length) //! value can be enumeration so call toString
                        {
                            throw new Exception(string.Format(
                                "({2})\n '{0}' Bu alana en fazla {1} adet harf yazýlabilir.",
                                property.Name, definition.Length, type.Name));
                        }
                    }

                    //Required validation
                    if (definition.IsRequired)
                        ValidateRequiredField(value, definition.Text);
                }
            }
        }

        protected void ValidateRequiredField(object value, string fieldName)
        {
            if (value==null)
                throw new Exception("Lütfen '" + fieldName + "' Alanýný Boþ Býrakmayýnýz.");
            
            Type type = value.GetType();
            if (type.IsSubclassOf(typeof(SbEntity))
                && !((SbEntity)value).Exist())
            {
                throw new Exception("Lütfen '" + fieldName + "' Alanýný Boþ Býrakmayýnýz.");
            }
            else if (type == typeof(string) && string.IsNullOrEmpty((string)value))
            {
                throw new Exception("Lütfen '" + fieldName + "' Alanýný Boþ Býrakmayýnýz.");
            }
            else if (string.IsNullOrEmpty(value.ToString()))
            {
                throw new Exception("Lütfen '" + fieldName + "' Alanýný Boþ Býrakmayýnýz.");
            }
        }

        public virtual void Insert()
        {
            Validate();
            Transaction.Instance.Join(delegate()
            {
                Id = Convert.ToInt32(Persistence.Insert(this));
            }, delegate()
            {
                this.Id = 0;
            },
            null, false, -1);
        }

        public virtual int Update()
        {
            Validate();
            int i = -1;
            Transaction.Instance.Join(delegate()
            {
                i = Persistence.Update(this);
                if (i != 1)
                    throw new SbUpdateException() { EntityType = this.GetType(), Id = this.Id };
            });

            return i;
        }

        public virtual void Save()
        {
            if (Exist())
                Update();
            else
                Insert();
        }

        public virtual void Delete()
        {
            Type typeOfInstance = this.GetType();
            bool throwException = true;

            Persistence.DeleteByKey(typeOfInstance, Id, throwException);
        }

        public virtual Type GetChildType()
        {
            return null;
        }

        public string GetName()
        {
            return this.GetType().Name;
        }

        public object GetValue(string fieldName)
        {
            return ReflectionHelper.GetValue(this, fieldName);
        }

        public object GetValueString(string fieldName)
        {
            object value;

            value = ReflectionHelper.GetValue(this, fieldName);
            if (value is DateTime)
                value = ((DateTime)value).ToString("dd.MM.yyyy");
            
            return value;
        }

        public void SetValue(string fieldName, object value)
        {
            ReflectionHelper.SetValue(this, fieldName, value);
        }

        public virtual bool Exist()
        {
            return id != 0;

            /*if (Id == null) return false;

            if (Id is string && string.IsNullOrEmpty((string)Id)) return false;
            if (Id is Int32 && ((Int32)Id) <= 0) return false;
            if (Id is Int64 && ((Int64)Id) <= 0) return false;
            
            return true;*/
        }

        public virtual bool NotExist()
        {
            return !Exist();
        }

        public virtual void Read()
        {
            Read(false);
        }

        public virtual void Read(bool forceRead)
        {
            if (!forceRead)
            {
                if (isRead) return;
                isRead = true;
            }
            Persistence.Read(this, Id);
        }
    }


    [Serializable]
    public class SbUpdateException : Exception
    {
        public Type EntityType { get; set; }
        public int Id { get; set; }

        public SbUpdateException() { }
        public SbUpdateException(string message) : base(message) { }
        public SbUpdateException(string message, Exception inner) : base(message, inner) { }

        protected SbUpdateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}