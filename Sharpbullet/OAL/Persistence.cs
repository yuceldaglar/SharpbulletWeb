using System;
using System.Collections.Generic;
using System.Text;
using SharpBullet.DAL;
using System.Data;
using SharpBullet.OAL.Metadata;
using SharpBullet.Helpers;

namespace SharpBullet.OAL
{
    public class Persistence
    {
        public static GetValueDelegate GetValue;
        public static SetValueDelegate SetValue;

        #region Single Read
        public static T Read<T>(object primaryKey)
        {
            Type type;
            type = typeof(T);

            return (T)Read(type, primaryKey);
        }

        public static object Read(Type type, object primaryKey)
        {
            object entityObject = Activator.CreateInstance(type);

            return Read(entityObject, primaryKey);
        }

        public static object Read(object entity, string sql, params object[] parameterValues)
        {
            Type type = entity.GetType();
            DataTable table;
            PersistenceStrategy strategy;

            strategy = PersistenceStrategyProvider.FindStrategyFor(type);

            table = Transaction.Instance.ExecuteSql(sql, parameterValues);
            if (table.Rows.Count > 0)
            {
                strategy.Fill(entity, table.Rows[0]);
            }
            else
                entity = null;

            return entity;
        }

        public static object Read(object entity, object primaryKey)
        {
            Type type = entity.GetType();
            DataTable table;
            PersistenceStrategy strategy;
            string tableName, keyColumn, keyParamName, sql;
            string[] fieldNames;

            strategy = PersistenceStrategyProvider.FindStrategyFor(type);
            tableName = strategy.GetTableNameOf(type);
            keyColumn = strategy.GetKeyColumnOf(type);

            sql = strategy.GetSelectStatementFor(type, new string[] { keyColumn }, new string[] { "@prm0" });

            //For types which are not views, sql is null or empty.
            if (string.IsNullOrEmpty(sql))
            {
                keyParamName = Transaction.Instance.SqlHelper().GenerateParamName(0);
                fieldNames = strategy.GetSelectFieldNamesOf(type);

                sql = Transaction.Instance.SqlHelper().BuildSelectSqlFor(tableName, fieldNames,
                    new string[] { keyColumn },
                    new string[] { keyParamName }, null, 0);
            }

            table = Transaction.Instance.ExecuteSql(sql, primaryKey);
            if (table.Rows.Count > 0)
            {
                strategy.Fill(entity, table.Rows[0]);
            }
            else
                entity = null;

            return entity;
        }

        public static T Read<T>(params Condition[] parameters)
        {
            T entity;
            T[] entityList;

            entityList = ReadList<T>(null, parameters, null, 1);
            if (entityList.Length > 0)
                entity = entityList[0];
            else
                entity = default(T);

            return entity;
        }

        public static T Read<T>(string sql, params object[] parameters)
        {
            T entity;
            T[] entityList;

            entityList = ReadList<T>(sql, parameters);
            if (entityList.Length > 0)
                entity = entityList[0];
            else
                entity = default(T);

            return entity;
        }

        public static T Where<T>(string whereStatement, params object[] parameters)
        {
            return Read<T>("select * from $this where " + whereStatement, parameters);
        }
        #endregion

        #region Multiple Read
        public static DataTable ReadListTable(Type entityType, string[] fields, Condition[] parameters, string[] orders, int limitNumberOfEntities)
        {
            PersistenceStrategy strategy;
            string tableName, sql, paramPrefix, paramSuffix;
            string[] filterFields, filterParams;
            object[] parameterValues;
            DataTable table;
            Operator[] operators;

            strategy = PersistenceStrategyProvider.FindStrategyFor(entityType);
            tableName = strategy.GetTableNameOf(entityType);
            filterFields = StrHelper.GetPropertyValuesOf(parameters, "Field");
            filterParams = StrHelper.GetNumbers(0, filterFields.Length);
            parameterValues = ArrayHelper.GetPropertyValuesOf(parameters, "Value");
            operators = ArrayHelper.GetPropertyValuesOf<Operator>(parameters, "Operator");

            paramPrefix = Transaction.Instance.SqlHelper().ParameterPrefix();
            paramSuffix = Transaction.Instance.SqlHelper().ParameterSuffix();
            filterParams = StrHelper.Concat(paramPrefix, filterParams, paramSuffix);

            sql = Transaction.Instance.SqlHelper().BuildSelectSqlFor(tableName, fields, filterFields, operators, filterParams, orders, limitNumberOfEntities);

            table = Transaction.Instance.ExecuteSql(sql, parameterValues);

            return table;
        }

        public static T[] ReadList<T>(string[] fields, Condition[] parameters, string[] orders, int limitNumberOfEntities)
        {
            Type type;
            T[] entities;
            PersistenceStrategy strategy;
            DataTable table;

            type = typeof(T);
            strategy = PersistenceStrategyProvider.FindStrategyFor(type);
            table = ReadListTable(type, fields, parameters, orders, limitNumberOfEntities);

            if (table.Rows.Count > 0)
            {
                entities = new T[table.Rows.Count];
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    entities[i] = Activator.CreateInstance<T>();
                    strategy.Fill(entities[i], table.Rows[i]);
                }
            }
            else
                entities = new T[0];

            return entities;
        }

        /// <summary>
        /// Tüm kayýtlarý okur. 'Select * from tablo'
        /// </summary>
        /// <typeparam name="T">Okunmasý istenen Entity</typeparam>
        /// <returns>Sonuçlarý dizi olarak döndürür</returns>
        public static T[] ReadList<T>()
        {
            return ReadList<T>(null);
        }

        public static T[] ReadList<T>(string sql, params object[] parameterValues)
        {
            return ReadList<T>(sql, null, parameterValues);
        }
        public static T[] ReadList<T>(string sql, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            Type type;
            T[] entities;
            PersistenceStrategy strategy;
            DataTable table;

            type = typeof(T);
            strategy = PersistenceStrategyProvider.FindStrategyFor(type);
            if (string.IsNullOrEmpty(sql))
                sql = string.Format("select * from {0}", strategy.GetTableNameOf(type));

            // Generic kullanýmýna ilave bir fayda, böylece tablo adý geçmemiþ olacak
            if (sql.Contains("$this"))
                sql = sql.Replace("$this", strategy.GetTableNameOf(type));

            if (sql.StartsWith("/*cache")
                && Transaction.Instance.Cache.ContainsKey(sql))
            {
                table = Transaction.Instance.Cache[sql];
            }
            else
            {
                table = Transaction.Instance.ExecuteSql(sql, namedParameters, parameterValues);
                if (sql.StartsWith("/*cache"))
                {
                    Transaction.Instance.Cache[sql] = table;
                }
            }
            if (table.Rows.Count > 0)
            {
                entities = new T[table.Rows.Count];
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    entities[i] = Activator.CreateInstance<T>();
                    strategy.Fill(entities[i], table.Rows[i]);
                }
            }
            else
                entities = new T[0];

            return entities;
        }

        public static T[] ReadDetail<T>(string parent, object id)
        {
            return ReadList<T>(null, new Condition[] { new Condition(parent, Operator.Equal, id) }, null, 0);
        }
        #endregion

        #region Save Methods
        public static object Insert(object entity)
        {
            Type type;
            PersistenceStrategy strategy;
            string tableName, sql, idSql, paramPrefix, paramSuffix;
            string[] fieldNames, parameterNames;
            object[] fieldValues;
            int i;
            IParameterHandle idParam = null;
            IdMethod idMethod;
            object idValue = 0;

            type = entity.GetType();
            strategy = PersistenceStrategyProvider.FindStrategyFor(type);
            paramPrefix = Transaction.Instance.SqlHelper().ParameterPrefix();
            paramSuffix = Transaction.Instance.SqlHelper().ParameterSuffix();

            tableName = strategy.GetTableNameOf(type);
            fieldNames = strategy.GetInsertFieldNamesOf(type);
            parameterNames = StrHelper.GetNumbers(0, fieldNames.Length);
            parameterNames = StrHelper.Concat(paramPrefix, parameterNames, paramSuffix);
            fieldValues = strategy.GetFieldValuesOf(entity, fieldNames);


            sql = Transaction.Instance.SqlHelper().BuildInsertSqlFor(tableName, fieldNames, parameterNames);

            idMethod = strategy.GetIdMethodFor(type);
            switch (idMethod)
            {
                case IdMethod.Identity:
                    idValue = 0;
                    idParam = Transaction.Instance.NewParameter("NewId", idValue, ParameterDirection.Output);
                    sql = Transaction.Instance.SqlHelper().
                        BuildInsertSqlWithIdentity(tableName, fieldNames, parameterNames, "NewId");
                    break;
                case IdMethod.BySql:
                    idSql = strategy.GetIdSqlFor(type);
                    idValue = Transaction.Instance.ExecuteScalar(idSql);
                    break;
                case IdMethod.Custom:
                    idValue = strategy.GetIdFor(entity, Transaction.Instance);
                    break;
                case IdMethod.UserSubmitted:
                    //biþey yapmaya gerek yok.
                    break;
            }

            if (Transaction.Instance.SqlHelper().GetType() == typeof(MySqlHelper))
            {
                //MySql için output parametreler ile ilgili sorun var!!!
                idParam.Value = Transaction.Instance.ExecuteScalar(sql, fieldValues);
                i = 1; //*** 
            }
            else
            {
                i = Transaction.Instance.ExecuteNonQuery(sql, null, fieldValues, idParam);
            }

            if (idParam != null)
                idValue = idParam.Value; //this works when 'idMethod' is '..Identity'

            return idValue;
        }

        public static int Update(object entity)
        {
            Type type;
            PersistenceStrategy strategy;
            string tableName, sql, keyField, keyParameter, paramPrefix, paramSuffix,
                optimisticLockField;
            string[] fieldNames, parameterNames;
            object[] fieldValues;
            object keyValue;
            byte optimisticLockValue;
            int i;

            type = entity.GetType();
            strategy = PersistenceStrategyProvider.FindStrategyFor(type);

            tableName = strategy.GetTableNameOf(type);
            fieldNames = strategy.GetUpdateFieldNamesOf(type);
            parameterNames = StrHelper.GetNumbers(0, fieldNames.Length);

            paramPrefix = Transaction.Instance.SqlHelper().ParameterPrefix();
            paramSuffix = Transaction.Instance.SqlHelper().ParameterSuffix();
            parameterNames = StrHelper.Concat(paramPrefix, parameterNames, paramSuffix);

            keyField = strategy.GetKeyColumnOf(type);
            keyParameter = paramPrefix + fieldNames.Length;
            keyValue = strategy.GetKeyValueOf(entity);

            optimisticLockField = strategy.GetOptimisticLockField(type);
            optimisticLockValue = 0;
            if (!string.IsNullOrEmpty(optimisticLockField))
                optimisticLockValue = (byte)strategy.GetOptimisticLockValue(entity);

            fieldValues = strategy.GetFieldValuesOf(entity, fieldNames);
            ArrayHelper.Merge<object>(ref fieldValues, keyValue);

            sql = Transaction.Instance.SqlHelper().BuildUpdateSqlFor(tableName, keyField, keyParameter,
                optimisticLockField, optimisticLockValue,
                fieldNames, parameterNames);

            i = Transaction.Instance.ExecuteNonQuery(sql, fieldValues);
            return i;
        }
        #endregion

        #region Delete Methods
        public static void DeleteByKey<T>(object key, bool throwException)
        {
            DeleteByKey(typeof(T), key, throwException);
        }

        public static void DeleteByKey(Type entityType, object key, bool throwException)
        {
            PersistenceStrategy strategy;
            string tableName, keyField, sql, keyParamName, keyParamSql;
            IParameterHandle idParameter;
            int numberOfRows;

            strategy = PersistenceStrategyProvider.FindStrategyFor(entityType);
            tableName = strategy.GetTableNameOf(entityType);
            keyField = strategy.GetKeyColumnOf(entityType);

            keyParamName = "prmId";
            keyParamSql = Transaction.Instance.SqlHelper().GenerateParamName(keyParamName);

            sql = Transaction.Instance.SqlHelper().BuildDeleteSqlFor(tableName, keyField, keyParamSql);
            idParameter = Transaction.Instance.NewParameter(keyParamName, key, ParameterDirection.Input);

            numberOfRows = Transaction.Instance.ExecuteNonQuery(sql, null, ArrayHelper.EmptyArray, idParameter);
        }
        #endregion
    }

    public class PersistenceObject
    {
        public Transaction Transaction { get; set; }

        public GetValueDelegate GetValue;
        public SetValueDelegate SetValue;

        #region Single Read
        public T Read<T>(object primaryKey)
        {
            Type type;
            type = typeof(T);

            return (T)Read(type, primaryKey);
        }

        public object Read(Type type, object primaryKey)
        {
            object entityObject = Activator.CreateInstance(type);

            return Read(entityObject, primaryKey);
        }

        public object Read(Type type, string sql, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            object entityObject = Activator.CreateInstance(type);

            return Read(entityObject, sql, namedParameters, parameterValues);
        }

        public object Read(object entity, string sql, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            Type type = entity.GetType();
            DataTable table;
            PersistenceStrategy strategy;

            strategy = PersistenceStrategyProvider.FindStrategyFor(type);

            table = Transaction.ExecuteSql(sql, namedParameters, parameterValues);
            if (table.Rows.Count > 0)
            {
                strategy.Fill(entity, table.Rows[0]);
            }
            else
                entity = null;

            return entity;
        }

        public object Read(object entity, object primaryKey)
        {
            Type type = entity.GetType();
            DataTable table;
            PersistenceStrategy strategy;
            string tableName, keyColumn, keyParamName, sql;
            string[] fieldNames;

            strategy = PersistenceStrategyProvider.FindStrategyFor(type);
            tableName = strategy.GetTableNameOf(type);
            keyColumn = strategy.GetKeyColumnOf(type);

            sql = strategy.GetSelectStatementFor(type, new string[] { keyColumn }, new string[] { "@prm0" });

            //For types which are not views, sql is null or empty.
            if (string.IsNullOrEmpty(sql))
            {
                keyParamName = Transaction.SqlHelper().GenerateParamName(0);
                fieldNames = strategy.GetSelectFieldNamesOf(type);

                sql = Transaction.SqlHelper().BuildSelectSqlFor(tableName, fieldNames,
                    new string[] { keyColumn },
                    new string[] { keyParamName }, null, 0);
            }

            table = Transaction.ExecuteSql(sql, primaryKey);
            if (table.Rows.Count > 0)
            {
                strategy.Fill(entity, table.Rows[0]);
            }
            else
                entity = null;

            return entity;
        }

        public T Read<T>(params Condition[] parameters)
        {
            T entity;
            T[] entityList;

            entityList = ReadList<T>(null, parameters, null, 1);
            if (entityList.Length > 0)
                entity = entityList[0];
            else
                entity = default(T);

            return entity;
        }

        public T Read<T>(string sql, params object[] parameters)
        {
            T entity;
            T[] entityList;

            entityList = ReadList<T>(sql, parameters);
            if (entityList.Length > 0)
                entity = entityList[0];
            else
                entity = default(T);

            return entity;
        }
        #endregion

        #region Multiple Read
        public DataTable ReadListTable(Type entityType, string[] fields, Condition[] parameters, string[] orders, int limitNumberOfEntities)
        {
            PersistenceStrategy strategy;
            string tableName, sql, paramPrefix, paramSuffix;
            string[] filterFields, filterParams;
            object[] parameterValues;
            DataTable table;
            Operator[] operators;

            strategy = PersistenceStrategyProvider.FindStrategyFor(entityType);
            tableName = strategy.GetTableNameOf(entityType);
            filterFields = StrHelper.GetPropertyValuesOf(parameters, "Field");
            filterParams = StrHelper.GetNumbers(0, filterFields.Length);
            parameterValues = ArrayHelper.GetPropertyValuesOf(parameters, "Value");
            operators = ArrayHelper.GetPropertyValuesOf<Operator>(parameters, "Operator");

            paramPrefix = Transaction.SqlHelper().ParameterPrefix();
            paramSuffix = Transaction.SqlHelper().ParameterSuffix();
            filterParams = StrHelper.Concat(paramPrefix, filterParams, paramSuffix);

            sql = Transaction.SqlHelper().BuildSelectSqlFor(tableName, fields, filterFields, operators, filterParams, orders, limitNumberOfEntities);

            table = Transaction.ExecuteSql(sql, parameterValues);

            return table;
        }

        public T[] ReadList<T>(string[] fields, Condition[] parameters, string[] orders, int limitNumberOfEntities)
        {
            Type type;
            T[] entities;
            PersistenceStrategy strategy;
            DataTable table;

            type = typeof(T);
            strategy = PersistenceStrategyProvider.FindStrategyFor(type);
            table = ReadListTable(type, fields, parameters, orders, limitNumberOfEntities);

            if (table.Rows.Count > 0)
            {
                entities = new T[table.Rows.Count];
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    entities[i] = Activator.CreateInstance<T>();
                    strategy.Fill(entities[i], table.Rows[i]);
                }
            }
            else
                entities = new T[0];

            return entities;
        }

        /// <summary>
        /// Tüm kayýtlarý okur. 'Select * from tablo'
        /// </summary>
        /// <typeparam name="T">Okunmasý istenen Entity</typeparam>
        /// <returns>Sonuçlarý dizi olarak döndürür</returns>
        public T[] ReadList<T>()
        {
            return ReadList<T>(null);
        }

        public T[] ReadList<T>(string sql, params object[] parameterValues)
        {
            return ReadList<T>(sql, null, parameterValues);
        }

        public T[] ReadList<T>(string sql, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            Type type;
            T[] entities;
            PersistenceStrategy strategy;
            DataTable table;

            type = typeof(T);
            strategy = PersistenceStrategyProvider.FindStrategyFor(type);
            if (string.IsNullOrEmpty(sql))
                sql = string.Format("select * from {0}", strategy.GetTableNameOf(type));

            // Generic kullanýmýna ilave bir fayda, böylece tablo adý geçmemiþ olacak
            if (sql.Contains("$this"))
                sql = sql.Replace("$this", strategy.GetTableNameOf(type));

            table = Transaction.ExecuteSql(sql, namedParameters, parameterValues);

            if (table.Rows.Count > 0)
            {
                entities = new T[table.Rows.Count];
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    entities[i] = Activator.CreateInstance<T>();
                    strategy.Fill(entities[i], table.Rows[i]);
                }
            }
            else
                entities = new T[0];

            return entities;
        }

        public List<object> ReadList(Type entityType, string sql, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            List<object> entities = new List<object>();
            PersistenceStrategy strategy;
            DataTable table;

            strategy = PersistenceStrategyProvider.FindStrategyFor(entityType);
            if (string.IsNullOrEmpty(sql))
                sql = string.Format("select * from {0}", strategy.GetTableNameOf(entityType));

            // Generic kullanýmýna ilave bir fayda, böylece tablo adý geçmemiþ olacak
            if (sql.Contains("$this"))
                sql = sql.Replace("$this", strategy.GetTableNameOf(entityType));

            table = Transaction.ExecuteSql(sql, namedParameters, parameterValues);

            if (table.Rows.Count > 0)
            {
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    object entityObject = Activator.CreateInstance(entityType);
                    strategy.Fill(entityObject, table.Rows[i]);
                    entities.Add(entityObject);
                }
            }

            return entities;
        }

        public T[] ReadDetail<T>(string parent, object id)
        {
            return ReadList<T>(null, new Condition[] { new Condition(parent, Operator.Equal, id) }, null, 0);
        }
        #endregion

        #region Save Methods
        public object Insert(object entity)
        {
            Type type;
            PersistenceStrategy strategy;
            string tableName, sql, idSql, paramPrefix, paramSuffix;
            string[] fieldNames, parameterNames;
            object[] fieldValues;
            int i;
            IParameterHandle idParam = null;
            IdMethod idMethod;
            object idValue = 0;

            type = entity.GetType();
            strategy = PersistenceStrategyProvider.FindStrategyFor(type);
            paramPrefix = Transaction.SqlHelper().ParameterPrefix();
            paramSuffix = Transaction.SqlHelper().ParameterSuffix();

            tableName = strategy.GetTableNameOf(type);
            fieldNames = strategy.GetInsertFieldNamesOf(type);
            parameterNames = StrHelper.GetNumbers(0, fieldNames.Length);
            parameterNames = StrHelper.Concat(paramPrefix, parameterNames, paramSuffix);
            fieldValues = strategy.GetFieldValuesOf(entity, fieldNames);


            sql = Transaction.SqlHelper().BuildInsertSqlFor(tableName, fieldNames, parameterNames);

            idMethod = strategy.GetIdMethodFor(type);
            switch (idMethod)
            {
                case IdMethod.Identity:
                    idValue = 0;
                    idParam = Transaction.NewParameter("NewId", idValue, ParameterDirection.Output);
                    sql = Transaction.SqlHelper().
                        BuildInsertSqlWithIdentity(tableName, fieldNames, parameterNames, "NewId");
                    break;
                case IdMethod.BySql:
                    idSql = strategy.GetIdSqlFor(type);
                    idValue = Transaction.ExecuteScalar(idSql);
                    break;
                case IdMethod.Custom:
                    idValue = strategy.GetIdFor(entity, Transaction.Instance);
                    break;
                case IdMethod.UserSubmitted:
                    //biþey yapmaya gerek yok.
                    break;
            }

            if (Transaction.SqlHelper().GetType() == typeof(MySqlHelper))
            {
                //MySql için output parametreler ile ilgili sorun var!!!
                idParam.Value = Transaction.ExecuteScalar(sql, fieldValues);
                i = 1; //*** 
            }
            else
            {
                i = Transaction.ExecuteNonQuery(sql, null, fieldValues, idParam);
            }

            if (idParam != null)
                idValue = idParam.Value; //this works when 'idMethod' is '..Identity'

            return idValue;
        }

        public int Update(object entity)
        {
            Type type;
            PersistenceStrategy strategy;
            string tableName, sql, keyField, keyParameter, paramPrefix, paramSuffix,
                optimisticLockField;
            string[] fieldNames, parameterNames;
            object[] fieldValues;
            object keyValue;
            byte optimisticLockValue;
            int i;

            type = entity.GetType();
            strategy = PersistenceStrategyProvider.FindStrategyFor(type);

            tableName = strategy.GetTableNameOf(type);
            fieldNames = strategy.GetUpdateFieldNamesOf(type);
            parameterNames = StrHelper.GetNumbers(0, fieldNames.Length);

            paramPrefix = Transaction.SqlHelper().ParameterPrefix();
            paramSuffix = Transaction.SqlHelper().ParameterSuffix();
            parameterNames = StrHelper.Concat(paramPrefix, parameterNames, paramSuffix);

            keyField = strategy.GetKeyColumnOf(type);
            keyParameter = paramPrefix + fieldNames.Length;
            keyValue = strategy.GetKeyValueOf(entity);

            optimisticLockField = strategy.GetOptimisticLockField(type);
            optimisticLockValue = 0;
            if (!string.IsNullOrEmpty(optimisticLockField))
                optimisticLockValue = (byte)strategy.GetOptimisticLockValue(entity);

            fieldValues = strategy.GetFieldValuesOf(entity, fieldNames);
            ArrayHelper.Merge<object>(ref fieldValues, keyValue);

            sql = Transaction.SqlHelper().BuildUpdateSqlFor(tableName, keyField, keyParameter,
                optimisticLockField, optimisticLockValue,
                fieldNames, parameterNames);

            i = Transaction.ExecuteNonQuery(sql, fieldValues);
            return i;
        }
        #endregion

        #region Delete Methods
        public void DeleteByKey<T>(object key, bool throwException)
        {
            DeleteByKey(typeof(T), key, throwException);
        }

        public void DeleteByKey(Type entityType, object key, bool throwException)
        {
            PersistenceStrategy strategy;
            string tableName, keyField, sql, keyParamName, keyParamSql;
            IParameterHandle idParameter;
            int numberOfRows;

            strategy = PersistenceStrategyProvider.FindStrategyFor(entityType);
            tableName = strategy.GetTableNameOf(entityType);
            keyField = strategy.GetKeyColumnOf(entityType);

            keyParamName = "prmId";
            keyParamSql = Transaction.SqlHelper().GenerateParamName(keyParamName);

            sql = Transaction.SqlHelper().BuildDeleteSqlFor(tableName, keyField, keyParamSql);
            idParameter = Transaction.NewParameter(keyParamName, key, ParameterDirection.Input);

            numberOfRows = Transaction.ExecuteNonQuery(sql, null, ArrayHelper.EmptyArray, idParameter);
        }
        #endregion
    }
}