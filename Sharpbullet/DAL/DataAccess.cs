using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace SharpBullet.DAL
{
    internal class DataAccess
    {
        public static IConnectionHandle GetConnection(string connectionString, string dbType)
        {
            DbProviderFactory factory;
            DbConnection connection;
            ConnectionHandle handle;

            try
            {
                factory = GetFactory(dbType);
            }
            catch (Exception exception)
            {
                throw new Exception("DB: " + dbType, exception);
            }                
            
            connection = factory.CreateConnection();
            connection.ConnectionString = connectionString;
            
            handle = new ConnectionHandle(connection, factory);

            return handle;
        }

        public static string GetSchema(string connectionString, string dbType)
        {
            DbProviderFactory factory;
            DbConnection connection;
            
            try
            {
                factory = GetFactory(dbType);
            }
            catch (Exception exception)
            {
                throw new Exception("DB: " + dbType, exception);
            }

            connection = factory.CreateConnection();
            connection.ConnectionString = connectionString;
            
            connection.Open();
            string result = connection.Database;
            connection.Close();
            
            return result;
        }

        private static DbProviderFactory GetFactory(string dbType)
        {
            DbProviderFactory factory;

            //Güncelleme: web hosting için bu if'e gerek yok. Web config'e dbprovider eklemek gerekiyor...
            //Bu if web hosting de çalýþmasý için yazýldý.
            //if (dbType == "MySql.Data.MySqlClient")
            //    factory = new MySql.Data.MySqlClient.MySqlClientFactory();
            //else 
                if (dbType=="System.Data.SQLite")
                //Referans eklemeden(sharpbullet için), hem de sisteme kurmadan kullanýlabiliyor
                //factory = DbProviderFactories.GetFactory("System.Data.SQLite");
                factory = (DbProviderFactory)Activator.CreateInstance(Type.GetType("System.Data.SQLite.SQLiteFactory, System.Data.SQLite"));
            else
                factory = DbProviderFactories.GetFactory(dbType);

            return factory;
        }

        public static void Open(IConnectionHandle handle)
        {
            ConnectionHandle connection;

            connection = (ConnectionHandle)handle;
            connection.Open();
        }

        public static void Close(IConnectionHandle handle)
        {
            ConnectionHandle connection;

            connection = (ConnectionHandle)handle;
            connection.Close();
        }

        public static ITransactionHandle BeginTransaction(IConnectionHandle handle)
        {
            ConnectionHandle connection;
            ITransactionHandle transaction;

            connection = (ConnectionHandle)handle;
            transaction = connection.BeginTransaction();

            return transaction;
        }

        public static void Commit(ITransactionHandle handle)
        {
            TransactionHandle transaction;

            transaction = (TransactionHandle)handle;
            transaction.Commit(); //TODO: Define the function.
        }

        public static void Rollback(ITransactionHandle handle)
        {
            TransactionHandle transaction;

            transaction = (TransactionHandle)handle;
            transaction.RollBack();
        }


        public static DataTable ExecuteSql(IConnectionHandle handle, string query, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            DataSet ds=ExecuteSqlWithDataSet(handle, query, namedParameters, parameterValues);
            if (ds != null && ds.Tables.Count > 0)
                return ds.Tables[0];
            return null;
        }


        public static DataSet ExecuteSqlWithDataSet(IConnectionHandle handle, string query, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            ConnectionHandle connection;
            DbCommand command;            
            DbTransaction transaction = null;
            DataSet table = null;

            connection = (ConnectionHandle)handle;
            //Transaction kullanmadan sql çalýþtýrmak gerekebiliyor, bildiðim tek örnek "create database..." ihtiyacý...
            //Böyle bir ihtiyaç için direkt .DAL katmanýndaki metodlarý kullanmak gerekiyor.
            if (connection.TransactionHandle != null)
                transaction = ((TransactionHandle)connection.TransactionHandle).Transaction;

            using (DbDataAdapter adapter = connection.Factory.CreateDataAdapter())
            {
                table = new DataSet();
                
                command = connection.Factory.CreateCommand();
                command.CommandText = query;
                command.Connection = connection.Connection;
                command.Transaction = transaction;
                command.CommandTimeout = connection.TimeoutSeconds < 0 ? 30 : connection.TimeoutSeconds;

                command.Parameters.AddRange(CreateParameters(connection, namedParameters, parameterValues));

                adapter.SelectCommand = command;
                
                adapter.Fill(table);
            }

            return table;
        }

        public static int ExecuteNonQuery(IConnectionHandle handle, string query, Dictionary<string, object> namedParameters, object[] parameterValues, IParameterHandle[] extraParameters)
        {
            ConnectionHandle connection;
            DbTransaction transaction;
            DbCommand command;
            int numberOfRows;

            connection = (ConnectionHandle)handle;
            transaction = ((TransactionHandle)connection.TransactionHandle).Transaction;

            command = connection.Factory.CreateCommand();
            command.CommandText = query;
            command.Connection = connection.Connection;
            command.Transaction = transaction;

            command.CommandTimeout = connection.TimeoutSeconds < 0 ? 30 : connection.TimeoutSeconds;

            command.Parameters.AddRange(CreateParameters(connection, namedParameters, parameterValues));

            if(extraParameters!=null)
                foreach (ParameterHandle p in extraParameters)
                {
                    if (p == null) continue;
                    command.Parameters.Add(p.Parameter);
                }

            numberOfRows = command.ExecuteNonQuery();

            return numberOfRows;
        }

        public static object ExecuteScalar(IConnectionHandle handle, string query, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            ConnectionHandle connection;
            DbTransaction transaction;
            DbCommand command;
            object result;

            connection = (ConnectionHandle)handle;
            transaction = ((TransactionHandle)connection.TransactionHandle).Transaction;

            command = connection.Factory.CreateCommand();
            command.CommandText = query;
            command.Connection = connection.Connection;
            command.Transaction = transaction;
            command.CommandTimeout = connection.TimeoutSeconds < 0 ? 30 : connection.TimeoutSeconds;

            command.Parameters.AddRange(CreateParameters(connection, namedParameters, parameterValues));

            result = command.ExecuteScalar();

            return result;
        }

        public static decimal ExecuteScalarD(IConnectionHandle handle, string query, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            decimal result;

            result = Convert.ToDecimal(
                ExecuteScalar(handle, query, namedParameters, parameterValues));

            return result;
        }

        public static int ExecuteScalarI(IConnectionHandle handle, string query, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            int result;

            result = Convert.ToInt32(
                ExecuteScalar(handle, query, namedParameters, parameterValues));

            return result;
        }

        public static long ExecuteScalarL(IConnectionHandle handle, string query, Dictionary<string, object> namedParameters, params object[] parameterValues)
        {
            long result;

            result = Convert.ToInt64(
                ExecuteScalar(handle, query, namedParameters, parameterValues));

            return result;
        }


        public static DataTable MetaTableColumns(IConnectionHandle handle, string tablename)
        {
            ConnectionHandle connection;
            connection = (ConnectionHandle)handle;
            return connection.Connection.GetSchema("Columns", new string[] { null, null, tablename, null });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="name"></param>
        /// <param name="value">Must be not null!</param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static IParameterHandle NewParameter(string dbType, string name, object value, ParameterDirection direction)
        {
            DbProviderFactory factory;
            DbParameter parameter;
            IParameterHandle handle;

            factory = GetFactory(dbType);
            
            parameter = factory.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            parameter.Direction = direction;
            //parameter.DbType is automatically detected from type of value;

            handle = new ParameterHandle(parameter);

            return handle;
        }

        private static DbParameter[] CreateParameters(ConnectionHandle connection, Dictionary<string, object> namedParameters, object[] parameterValues)
        {
            DbParameter[] parameters = new DbParameter[0];

            parameterValues = parameterValues ?? new object[0];
            namedParameters = namedParameters ?? new Dictionary<string, object>();

            for (int i = 0; i < parameterValues.Length; i++)
            {
                namedParameters["prm" + i] = parameterValues[i];
            }

            int index = 0;
            if (parameterValues != null)
            {
                parameters = new DbParameter[namedParameters.Keys.Count];

                foreach (string key in namedParameters.Keys)
                {
                    object paramValue = namedParameters[key];

                    //Datetime mapping
                    if (paramValue is DateTime && DateTime.MinValue.Equals((DateTime)paramValue)) paramValue = DBNull.Value;
                    //null mapping
                    if (paramValue == null) paramValue = DBNull.Value;

                    DbParameter parameter = connection.Factory.CreateParameter();
                    parameter.ParameterName = key;
                    parameter.Value = paramValue;

                    parameters[index] = parameter;
                    index++;
                }
            }
            return parameters;
        }
    }
}