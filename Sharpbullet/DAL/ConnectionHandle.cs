using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace SharpBullet.DAL
{
    class ConnectionHandle : IConnectionHandle
    {
        public ConnectionHandle(DbConnection connection, DbProviderFactory factory)
        {
            this.connection = connection;
            this.factory = factory;

            RollbackMethods = new List<Action>();
            BeforeCommitMethods = new List<Action>();
            CommitMethods = new List<Action>();
        }

        private DbConnection connection;
        private DbProviderFactory factory;

        private TransactionHandle transactionHandle;

        public List<Action> RollbackMethods { get; set; }
        public List<Action> BeforeCommitMethods { get; set; }
        public List<Action> CommitMethods { get; set; }

        public ITransactionHandle TransactionHandle
        {
            get { return transactionHandle; }
        }

        public DbConnection Connection
        {
            get { return connection; }
        }

        public DbProviderFactory Factory
        {
            get { return factory; }
        }

        public ITransactionHandle BeginTransaction()
        {
            DbTransaction transaction;

            transaction = connection.BeginTransaction();
            transactionHandle = new TransactionHandle(transaction);

            return transactionHandle;
        }

        public int TimeoutSeconds { get; set; }

        public string Server { get { if (Connection == null) return ""; return Connection.DataSource; } }

        public string Database { get { if (Connection == null) return ""; return Connection.Database; } }

        private Dictionary<string, KeyValuePair<int, bool>> customizationRecords;
        public Dictionary<string, KeyValuePair<int, bool>> CustomizationRecords
        {
            get
            {
                if (customizationRecords == null) customizationRecords = new Dictionary<string, KeyValuePair<int, bool>>();
                return customizationRecords;
            }
            set
            {
                customizationRecords = value;
            }
        }

        internal void Open()
        {
            connection.Open();
        }

        internal void Close()
        {
            connection.Close();
        }
    }
}