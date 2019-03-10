using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using SharpBullet.DAL;
using System.Data;
using System.Threading;
using System.Diagnostics;
using SharpBullet.OAL.Metadata;
using SharpBullet.Helpers;

namespace SharpBullet.OAL
{
    public delegate Transaction GetTransaction();

    public class Transaction
    {
        public static string DefaultDbType { get; set; }
        public static string DefaultConnection { get; set; }

        public event EventHandler Committed;
        public int ThreadId { get; set; }

        private Dictionary<string, DataTable> cache;

        public Dictionary<string, DataTable> Cache
        {
            get
            {
                if (cache == null) cache = new Dictionary<string, DataTable>();
                return cache;
            }
            set { cache = value; }
        }

        /////// <summary>
        /////// Web ve Application server gibi ortamlardaki session mantýðýný desteklemek için kullanýlýr.
        /////// Yani set edilen T.Provider, session bazýnda bir nesne verebilmelidir.
        /////// </summary>
        ////public static GetTransaction TransactionProvider;

        private static ISqlHelper sqlHelper;
        private static string httpContextKey = "SharpBullet.Transaction";

        /// <summary>
        /// web'de tenant bazýnda connection ayarlayabilmek için tasarlandý.
        /// her request için set edilmesi gerekiyor, session bazlý deðil.
        /// </summary>
        
        public static string DB_TYPE = "SharpBullet.DbKey";
        public static string CONNECTION_STRING = "SharpBullet.ConnectionString";
        public static string ENTITY_CUSTOMIZATION = "SharpBullet.EntityCustomization";




        public static Transaction Instance
        {
            get
            {
                Transaction instance = null;

                //Asp.Net içinde SharpBullet kullanuýldýðýnda, her sayfa request'inin kendi transaction kuyruðu olmalý.
                if (System.Web.HttpContext.Current != null)
                {
                    if (!System.Web.HttpContext.Current.Items.Contains(httpContextKey))
                    {
                        string dbType = null;
                        string connectionString = null;

                        if (System.Web.HttpContext.Current.Items.Contains(DB_TYPE))
                            dbType = (string)System.Web.HttpContext.Current.Items[DB_TYPE];

                        if (System.Web.HttpContext.Current.Items.Contains(CONNECTION_STRING))
                            connectionString = (string)System.Web.HttpContext.Current.Items[CONNECTION_STRING];

                        var t = new Transaction(connectionString, dbType);

                        System.Web.HttpContext.Current.Items.Add(httpContextKey, t);
                    }
                    return (Transaction)System.Web.HttpContext.Current.Items[httpContextKey];
                }
                else
                {
                    int id = Thread.CurrentThread.ManagedThreadId;
                    if (!threadStack.ContainsKey(id))
                    {
                        threadStack[id] = new Transaction();
                        threadStack[id].ThreadId = id;
                    }
                    instance = threadStack[id];
                }

                return instance;
            }

            set
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                threadStack[id] = value;
                threadStack[id].ThreadId = id;
            }
        }

        private static Dictionary<int, Transaction> threadStack = new Dictionary<int, Transaction>();

        private string _connectionString, _dbType;

        public string DbType { get { return _dbType ?? DefaultDbType ?? "System.Data.SqlClient"; } }

        public string ConnectionString { get { return _connectionString ?? DefaultConnection; } }

        private Dictionary<string, byte> customizationEntities;

        private Dictionary<string, byte> CustomizationEntities
        {
            get
            {
                if (customizationEntities == null) customizationEntities = new Dictionary<string, byte>();
                return customizationEntities;
            }
        }

        private Stack<IConnectionHandle> stack = new Stack<IConnectionHandle>();




        public Transaction()
        {
        }

        public Transaction(string connectionString, string dbType)
        {
            this._dbType = dbType;
            this._connectionString = connectionString;
        }

        public static void SetConnectionSqlServer(string connectionString)
        {
            SetConnection("System.Data.SqlClient", connectionString);
        }

        public static void SetConnection(string dbType, string connectionString)
        {
            DefaultDbType = dbType;
            DefaultConnection = connectionString;
        }

        public void NewTransaction(Action run)
        {
            Join(run, null, null, true, -1);
        }

        public void RegisterForCustomization(string entityName, int id, bool update)
        {
            var connection = stack.Peek();

            if (!CustomizationEntities.ContainsKey(entityName)) return;

            if (connection.CustomizationRecords.ContainsKey(entityName)) return;

            connection.CustomizationRecords[entityName] = new KeyValuePair<int, bool>(id, update);
        }

        public void NewTransaction(Action run, Action rollback, Action commit, bool startNewTransaction, int timeoutSeconds)
        {
            Join(run, rollback, commit, startNewTransaction, timeoutSeconds);
        }

        public void Join(Action run)
        {
            Join(run, null, null, false, -1);
        }

        public void JoinBeforeCommit(Action method)
        {
            Join(null, null, method, null, false, -1);
        }

        public void Join(Action run, Action rollback, Action commit, bool startNewTransaction, int timeoutSeconds)
        {
            Join(run, rollback, null, commit, startNewTransaction, timeoutSeconds);
        }
                
        public void Join(Action run, Action rollback, Action beforeCommit, Action commit, bool startNewTransaction, int timeoutSeconds)
        {
            bool owner;
            IConnectionHandle connection = null;

            if (stack.Count == 0 || startNewTransaction) //If there is no transaction start a new one.
            {
                connection = DataAccess.GetConnection(ConnectionString, DbType);

                #region Timeout Ayarlarý
                var connectionStringHasTimeout = 
                    ConnectionString.Contains("ConnectTimeout") || ConnectionString.Contains("Connection Timeout");

                if (!connectionStringHasTimeout)
                {
                    connection.TimeoutSeconds = timeoutSeconds < 0 ? 300 : 0;
                }
                #endregion

                DataAccess.Open(connection);
                DataAccess.BeginTransaction(connection);
                stack.Push(connection);
                owner = true; //If this method call starts a new transaction, mark it as owner.
            }
            else
            {
                owner = false;
                connection = stack.Peek();
            }

            try
            {
                if (rollback != null)
                    connection.RollbackMethods.Add(rollback);

                if (beforeCommit != null)
                    connection.BeforeCommitMethods.Add(beforeCommit);

                if (commit != null)
                    connection.CommitMethods.Add(commit);

                if (run != null)
                {
                    run();
                }

                if (owner) /* bu bloðunda, bu try catch içinde kalmasý lazým */
                {
                    connection = stack.Peek();
                    foreach (var beforeCommitMethod in connection.BeforeCommitMethods)
                    {
                        beforeCommitMethod();
                    }

                    foreach (var item in connection.CustomizationRecords)
                    {
                        RunCustomization(item.Key, item.Value.Key, item.Value.Value);
                    }
                    connection.CustomizationRecords.Clear();
                }
            }
            catch (Exception ex)
            {
                if (owner)
                {
                    foreach (Action rollbackMethod in connection.RollbackMethods)
                    {
                        try
                        {
                            rollbackMethod();
                        }
                        catch { }
                    }
                    connection.RollbackMethods.Clear();
                    connection.CustomizationRecords.Clear();

                    connection = stack.Pop();
                    DataAccess.Rollback(connection.TransactionHandle);
                    DataAccess.Close(connection);
                }
                //TODO: Convert exception to a user friendly message. Conversion can take place in provider specific sql helper.
                throw;
            }

            if (owner)
            {
                connection = stack.Pop();
                DataAccess.Commit(connection.TransactionHandle);
                DataAccess.Close(connection);

                connection.RollbackMethods.Clear();

                foreach (Action commitMethod in connection.CommitMethods)
                {
                    try
                    {
                        commitMethod();
                    }
                    catch { }
                }
                connection.CommitMethods.Clear();

                if (Committed != null)
                    Committed(null, EventArgs.Empty);
            }
        }

        private void RunCustomization(string entityName, int id, bool update)
        {
            Transaction.Instance.ExecuteNonQuery(string.Format(@"
                        IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Save_{0}')
                        BEGIN
                            exec Save_{0} @id = @prm0, @update = @prm1
                        END
                    ", entityName), id, update);
        }

        public DataSet ExecuteSqlWithDataSet(string query, params object[] parameterValues)
        {
            return ExecuteSqlWithDataSet(query, null, parameterValues);
        }

        public DataSet ExecuteSqlWithDataSet(string query, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            IConnectionHandle connection;
            DataSet ds = null;

            if (!this.DbType.Contains(".SqlClient"))
                query = SqlHelper().Translate(query);

            Join(delegate()
            {
                connection = stack.Peek();
                ds = DataAccess.ExecuteSqlWithDataSet(connection, query, namedParameters, parameterValues);
            });

            return ds;
        }

        public DataTable ExecuteSql(string query, params object[] parameterValues)
        {
            return ExecuteSql(query, null, parameterValues);
        }

        public DataTable ExecuteSql(string query, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            return ExecuteSql(query, namedParameters, 0, parameterValues);
        }

        public DataTable ExecuteSql(string query, Dictionary<string, object> namedParameters, int timeoutSeconds, params object[] parameterValues)
        {
            IConnectionHandle connection;
            DataTable table = null;

            if (!this.DbType.Contains(".SqlClient"))
                query = SqlHelper().Translate(query);

            Join(delegate()
            {
                connection = stack.Peek();
                if (timeoutSeconds > 0)
                {
                    connection.TimeoutSeconds = timeoutSeconds;
                }

                table = DataAccess.ExecuteSql(connection, query, namedParameters, parameterValues);
            });

            return table;
        }

        private void OnExecuted(string query, Dictionary<string, object> namedParameters, object[] parameterValues, Stopwatch watch)
        {
            string declare = "";

            if (namedParameters != null)
            {
                foreach (string key in namedParameters.Keys)
                {
                    declare += "declare " + (key.StartsWith("@") ? key : "@" + key);

                    if (namedParameters[key] is DateTime)
                        declare += " datetime ='" + ((DateTime)namedParameters[key]).ToString("yyyyMMdd HH:mm:ss") + "'";
                    else if (namedParameters[key] is int)
                        declare += " int =" + namedParameters[key].ToString();
                    else if (namedParameters[key] is decimal)
                        declare += " decimal(38,9) =" + namedParameters[key].ToString();
                    else
                        declare += " varchar(" + namedParameters[key].ToString().Length + ")='" + namedParameters[key].ToString() + "'";

                    declare = declare + Environment.NewLine;
                }
            }

            if (parameterValues != null)
            {
                int i = 0;
                foreach (object value in parameterValues)
                {
                    declare += "declare @prm" + i++;

                    if (value == null) continue;

                    if (value is DateTime)
                        declare += " datetime ='" + ((DateTime)value).ToString("yyyyMMdd HH:mm:ss") + "'";
                    else if (value is int)
                        declare += " int =" + value.ToString();
                    else if (value is decimal)
                        declare += " decimal(38,9) =" + value.ToString();
                    else
                        declare += " varchar(" + value.ToString().Length + ")='" + value.ToString() + "'";

                    declare = declare + Environment.NewLine;
                }
            }
        }

        public int ExecuteNonQuery(string query, params object[] parameterValues)
        {
            return ExecuteNonQuery(query, null, parameterValues, null);
        }

        public int ExecuteNonQuery(string query, Dictionary<string, object> namedParameters, object[] parameterValues)
        {
            return ExecuteNonQuery(query, namedParameters, parameterValues, new IParameterHandle[0]);
        }

        public int ExecuteNonQuery(string query, Dictionary<string, object> namedParameters, object[] parameterValues, params IParameterHandle[] extraParameters)
        {
            IConnectionHandle connection;
            int i = 0;

            if (!this.DbType.Contains(".SqlClient"))
                query = SqlHelper().Translate(query);

            Join(delegate()
            {
                connection = stack.Peek();
                i = DataAccess.ExecuteNonQuery(connection, query, namedParameters, parameterValues, extraParameters);                
            });

            return i;
        }

        public int ExecuteNonQueryTimeout(string query, int timeoutSeconds, params object[] parameterValues)
        {
            return ExecuteNonQueryTimeout(query, timeoutSeconds, null, parameterValues, null);
        }

        public int ExecuteNonQueryTimeout(string query, int timeoutSeconds, Dictionary<string, object> namedParameters, object[] parameterValues)
        {
            return ExecuteNonQueryTimeout(query, timeoutSeconds, namedParameters, parameterValues, 0, null);
        }

        public int ExecuteNonQueryTimeout(string query, int timeoutSeconds, Dictionary<string, object> namedParameters, object[] parameterValues, params IParameterHandle[] extraParameters)
        {
            IConnectionHandle connection;
            int i = 0;

            if (!this.DbType.Contains(".SqlClient"))
                query = SqlHelper().Translate(query);

            Join(delegate ()
            {
                connection = stack.Peek();
                if (timeoutSeconds > 0)
                {
                    connection.TimeoutSeconds = timeoutSeconds;
                }

                i = DataAccess.ExecuteNonQuery(connection, query, namedParameters, parameterValues, extraParameters);                
            });

            return i;
        }


        public object ExecuteScalar(string query, params object[] parameterValues)
        {
            return ExecuteScalar(query, null, parameterValues);
        }

        public object ExecuteScalar(string query, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            IConnectionHandle connection;
            object result = null;

            if (!this.DbType.Contains(".SqlClient"))
                query = SqlHelper().Translate(query);

            Join(delegate()
            {
                connection = stack.Peek();
                result = DataAccess.ExecuteScalar(connection, query, namedParameters, parameterValues);                
            });

            return result;
        }

        public DateTime ExecuteScalarDT(string query, params object[] parameterValues)
        {
            DateTime result;

            result = Convert.ToDateTime(
                ExecuteScalar(query, parameterValues));

            return result;
        }

        public decimal ExecuteScalarD(string query, params object[] parameterValues)
        {
            decimal result = 0;

            object value = ExecuteScalar(query, parameterValues);
            if (value != DBNull.Value)
                result = Convert.ToDecimal(value);

            return result;
        }

        public int ExecuteScalarI(string query, params object[] parameterValues)
        {
            int result = 0;


            object value = ExecuteScalar(query, parameterValues);
            if (value != DBNull.Value)
                result = Convert.ToInt32(value);

            return result;
        }

        public long ExecuteScalarL(string query, params object[] parameterValues)
        {
            long result = 0;

            object value = ExecuteScalar(query, parameterValues);
            if (value != DBNull.Value)
                result = Convert.ToInt64(value);

            return result;
        }

        public string ExecuteScalarS(string query, params object[] parameterValues)
        {
            string result;

            object value = ExecuteScalar(query, parameterValues) ?? "";
            result = value.ToString();

            return result;
        }

        public List<T> Execute<T>(string query, params object[] parameterValues)
        {
            PersistenceStrategy strategy;
            Type type = typeof(T);
            strategy = PersistenceStrategyProvider.FindStrategyFor(type);

            DataTable dt = ExecuteSql(query, parameterValues);
            List<T> list = new List<T>();
            foreach (DataRow dr in dt.Rows)
            {
                T entity = Activator.CreateInstance<T>();
                strategy.Fill(entity, dr);
                list.Add(entity);
            }
            return list;
        }

        public IParameterHandle NewParameter(string name, object value, ParameterDirection direction)
        {
            IParameterHandle handle;
            handle = DataAccess.NewParameter(DbType, name, value, direction);

            return handle;
        }

        public string GetSchema()
        {
            return DataAccess.GetSchema(ConnectionString, DbType);
        }

        public ISqlHelper SqlHelper()
        {
            if (sqlHelper == null)
            {
                sqlHelper = SqlHelperProvider.FindHelperFor(DbType);
            }

            return sqlHelper;
        }

        public DataTable MetaTableColumns(string tablename)
        {
            DataTable result = null;
            IConnectionHandle connection;

            Join(delegate()
            {
                connection = stack.Peek();
                result = DataAccess.MetaTableColumns(connection, tablename);
            });
            return result;
        }

        #region *** PERSISTENCE ***
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

        public object Read(object entity, string sql, params object[] parameterValues)
        {
            Type type = entity.GetType();
            DataTable table;
            PersistenceStrategy strategy;

            strategy = PersistenceStrategyProvider.FindStrategyFor(type);

            table = this.ExecuteSql(sql, parameterValues);
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
                keyParamName = this.SqlHelper().GenerateParamName(0);
                fieldNames = strategy.GetSelectFieldNamesOf(type);

                sql = this.SqlHelper().BuildSelectSqlFor(tableName, fieldNames,
                    new string[] { keyColumn },
                    new string[] { keyParamName }, null, 0);
            }

            table = this.ExecuteSql(sql, primaryKey);
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

        public T Where<T>(string whereStatement, params object[] parameters)
        {
            return Read<T>("select * from $this where " + whereStatement, parameters);
        }

        public List<T> WhereAll<T>(string whereStatement, params object[] parameters)
        {
            var list = new List<T>();

            var result = ReadList<T>("select * from $this where " + whereStatement, parameters);
            if (result != null && result.Length > 0)
            {
                list.AddRange(result);
            }

            return list;
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

            paramPrefix = this.SqlHelper().ParameterPrefix();
            paramSuffix = this.SqlHelper().ParameterSuffix();
            filterParams = StrHelper.Concat(paramPrefix, filterParams, paramSuffix);

            sql = this.SqlHelper().BuildSelectSqlFor(tableName, fields, filterFields, operators, filterParams, orders, limitNumberOfEntities);

            table = this.ExecuteSql(sql, parameterValues);

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

            if (sql.StartsWith("/*cache")
                && this.Cache.ContainsKey(sql))
            {
                table = this.Cache[sql];
            }
            else
            {
                table = this.ExecuteSql(sql, namedParameters, parameterValues);
                if (sql.StartsWith("/*cache"))
                {
                    this.Cache[sql] = table;
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
            paramPrefix = this.SqlHelper().ParameterPrefix();
            paramSuffix = this.SqlHelper().ParameterSuffix();

            tableName = strategy.GetTableNameOf(type);
            fieldNames = strategy.GetInsertFieldNamesOf(type);
            parameterNames = StrHelper.GetNumbers(0, fieldNames.Length);
            parameterNames = StrHelper.Concat(paramPrefix, parameterNames, paramSuffix);
            fieldValues = strategy.GetFieldValuesOf(entity, fieldNames);


            sql = this.SqlHelper().BuildInsertSqlFor(tableName, fieldNames, parameterNames);

            idMethod = strategy.GetIdMethodFor(type);
            switch (idMethod)
            {
                case IdMethod.Identity:
                    idValue = 0;
                    idParam = this.NewParameter("NewId", idValue, ParameterDirection.Output);
                    sql = this.SqlHelper().
                        BuildInsertSqlWithIdentity(tableName, fieldNames, parameterNames, "NewId");
                    break;
                case IdMethod.BySql:
                    idSql = strategy.GetIdSqlFor(type);
                    idValue = this.ExecuteScalar(idSql);
                    break;
                case IdMethod.Custom:
                    idValue = strategy.GetIdFor(entity, Transaction.Instance);
                    break;
                case IdMethod.UserSubmitted:
                    //biþey yapmaya gerek yok.
                    break;
            }

            if (this.SqlHelper().GetType() == typeof(MySqlHelper))
            {
                //MySql için output parametreler ile ilgili sorun var!!!
                idParam.Value = this.ExecuteScalar(sql, fieldValues);
                i = 1; //*** 
            }
            else
            {
                i = this.ExecuteNonQuery(sql, null, fieldValues, idParam);
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

            paramPrefix = this.SqlHelper().ParameterPrefix();
            paramSuffix = this.SqlHelper().ParameterSuffix();
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

            sql = this.SqlHelper().BuildUpdateSqlFor(tableName, keyField, keyParameter,
                optimisticLockField, optimisticLockValue,
                fieldNames, parameterNames);

            i = this.ExecuteNonQuery(sql, fieldValues);
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
            keyParamSql = this.SqlHelper().GenerateParamName(keyParamName);

            sql = this.SqlHelper().BuildDeleteSqlFor(tableName, keyField, keyParamSql);
            idParameter = this.NewParameter(keyParamName, key, ParameterDirection.Input);

            numberOfRows = this.ExecuteNonQuery(sql, null, ArrayHelper.EmptyArray, idParameter);
        }
        #endregion
        #endregion

        #region Helpers
        public void DeleteFrom<T>(string where, params object[] parameterValues)
        {
            if (string.IsNullOrEmpty(where))
                throw new ApplicationException("Silme iþleminde þart belirtilmemiþ. Tablo adý: " + typeof(T).Name);

            string sql = string.Format("delete from {0} where {1}", typeof(T).Name, where);

            ExecuteNonQuery(sql, parameterValues);
        }

        public void DeleteTopFrom<T>(int top, string where, params object[] parameterValues)
        {
            if (string.IsNullOrEmpty(where))
                throw new ApplicationException("Silme iþleminde þart belirtilmemiþ. Tablo adý: " + typeof(T).Name);

            string sql = string.Format("delete top({0}) from {1} where {2}", top, typeof(T).Name, where);

            ExecuteNonQuery(sql, parameterValues);
        }

        public T[] ReadListFrom<T>(string where, params object[] parameterValues)
        {
            var sql = "select * from " + typeof(T).Name;
            if (!string.IsNullOrEmpty(where)) sql += " where " + where;

            return ReadList<T>(sql, parameterValues);
        }
        #endregion
    }

    public delegate object GetValueDelegate(string key);
    public delegate void SetValueDelegate(string key, object value);
}
