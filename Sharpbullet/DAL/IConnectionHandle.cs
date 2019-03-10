using System;
using System.Collections.Generic;
using System.Text;

namespace SharpBullet.DAL
{
    internal interface IConnectionHandle
    {
        ITransactionHandle TransactionHandle { get; }

        string Server { get; }

        string Database { get; }

        int TimeoutSeconds { get; set; }

        Dictionary<string, KeyValuePair<int, bool>> CustomizationRecords { get; set; }
        List<Action> RollbackMethods { get; set; }
        List<Action> BeforeCommitMethods { get; set; }
        List<Action> CommitMethods { get; set; }
    }
}