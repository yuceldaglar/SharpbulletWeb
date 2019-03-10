using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using SharpBullet.OAL.Metadata;
using SharpBullet.Entities;

namespace SharpBullet.OAL.Schema
{
    public static class SchemaHelper
    {
        public static string Migrate(Type baseEntityType, bool run)
        {
            System.Reflection.Assembly assembly = baseEntityType.Assembly;
            return Migrate(baseEntityType, run, Transaction.Instance, assembly);
        }

        public static string Migrate(Type baseEntityType, bool run, Transaction transaction, Assembly assembly)
        {
            StringBuilder str = new StringBuilder();
            if (transaction.DbType.Contains("Mysql"))
                updateMysql(str, assembly, baseEntityType, run, transaction);
            else
                updateMs(str, assembly, baseEntityType, run, transaction);

            return str.ToString();
        }

        public static string ConvertEnums(Assembly assembly, Type baseEntityType, bool run)
        {
            string result = "";
            string sql = @"UPDATE {0} SET {1}={2} WHERE {1}='{3}'";

            foreach (Type entityType in assembly.GetTypes())
            {
                if (!entityType.IsClass
                    || !entityType.IsSubclassOf(baseEntityType)
                    || entityType.IsGenericType
                    || entityType.IsAbstract
                    || entityType.Name.Contains("<")) continue;

                PropertyInfo[] props =
                    PersistenceStrategyProvider.FindStrategyFor(entityType)
                    .GetPersistentProperties(entityType);

                string className = entityType.Name;

                System.Collections.Hashtable fieldDictionary = new System.Collections.Hashtable();
                foreach (PropertyInfo property in props)
                {
                    if (!property.PropertyType.IsEnum) continue;
                    
                    foreach (var item in Enum.GetValues(property.PropertyType))
                    {
                        result +=
                            string.Format(sql, className, property.Name, (int)item, item.ToString())
                            + Environment.NewLine;
                    }
                }
            }

            return result;
        }

        private static void updateMs(StringBuilder str, Assembly assembly, Type baseEntityType, bool run, Transaction transaction)
        {
            StringBuilder keys = new System.Text.StringBuilder();

            foreach (Type entityType in assembly.GetTypes())
            {
                if (!entityType.IsClass
                    || !entityType.IsSubclassOf(baseEntityType)
                    || entityType.IsGenericType
                    || entityType.IsAbstract
                    || entityType.Name.Contains("<")) continue;

                var np = entityType.GetCustomAttributes(typeof(NonPersistentAttribute), false);
                if (np != null && np.Length > 0) continue;

                PropertyInfo[] props =
                    PersistenceStrategyProvider.FindStrategyFor(entityType)
                    .GetPersistentProperties(entityType);

                string className = entityType.Name;
                if (TableExistMs(className, transaction))
                {
                    DataTable table = transaction.ExecuteSql("select * from " + className + " where Id=-1");

                    System.Collections.Hashtable fieldDictionary = new System.Collections.Hashtable();
                    foreach (PropertyInfo property in props)
                    {
                        if (!property.CanWrite) continue;

                        bool isEntityPointer = property.PropertyType.IsGenericType
                            && property.PropertyType.GetGenericTypeDefinition() == typeof(SharpPointer<>);

                        FieldDefinitionAttribute definition = DataDictionary.Instance.GetFieldDefinition(entityType, property);
                        string fieldname = (isEntityPointer || property.PropertyType.IsSubclassOf(baseEntityType)) 
                            ? definition.Name + "_Id" 
                            : definition.Name;

                        var propType = property.PropertyType.IsGenericType ? property.PropertyType.GetGenericArguments()[0] : property.PropertyType;

                        fieldDictionary[fieldname] = 1;
                        if (!table.Columns.Contains(fieldname))
                        {
                            string sql = "alter table " + className + " add " + fieldname + " " + MapType(definition);
                            if (property.PropertyType.IsEnum)
                            {
                                sql += " not null default 0";
                            }

                            if (run)
                                transaction.ExecuteNonQuery(sql);

                            string fkSql = "";
                            if (fieldname.EndsWith("_Id"))
                            {
                                fkSql = string.Format(@"ALTER TABLE {0} ADD CONSTRAINT
                                	FK_{0}_{2} FOREIGN KEY
	                                ({2}) REFERENCES {1}(Id) ON UPDATE  NO ACTION ON DELETE  NO ACTION", className, propType.Name, fieldname);
                                keys.Append(fkSql + Environment.NewLine + Environment.NewLine);
                            }

                            str.Append("-- New Field: " + fieldname + ", Table: " + className + Environment.NewLine);
                            str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                        }
                        else if (!fieldname.EndsWith("_Id") && table.Columns[fieldname].DataType.Name == "String"
                            && definition.TypeName == "Text")
                        {
                            // do nothing
                        }
                        else if (!fieldname.EndsWith("_Id") && table.Columns[fieldname].DataType.Name == "String"
                            && definition.TypeName == "String")
                        {
                            int maxUzunlunluk = transaction.ExecuteScalarI(
                                @"SELECT CHARACTER_MAXIMUM_LENGTH
                                    FROM INFORMATION_SCHEMA.COLUMNS
                                    WHERE TABLE_SCHEMA = 'dbo'
		                                    AND TABLE_NAME = @prm0   
		                                    AND COLUMN_NAME = @prm1",
                                className, fieldname);
                            if (!(maxUzunlunluk == -1 && definition.Length == 0) && maxUzunlunluk < definition.Length)
                            {
                                string sql = "alter table " + className + " alter column " + fieldname + " " + MapType(definition);
                                if (run)
                                    transaction.ExecuteNonQuery(sql);

                                str.Append("-- Alter Field: " + fieldname + ", Table: " + className + Environment.NewLine);
                                str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                            }
                        }
                        else if (!fieldname.EndsWith("_Id") && table.Columns[fieldname].DataType.Name != definition.TypeName)
                        {
                            string sql = "alter table " + className + " alter column " + fieldname + " " + MapType(definition);
                            if (run)
                                transaction.ExecuteNonQuery(sql);

                            str.Append("-- Alter Field: " + fieldname + ", Table: " + className + Environment.NewLine);
                            str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                        }
                        else if (table.Columns[fieldname].DataType == typeof(decimal))
                        {
                            DataTable decimalColumn = transaction.ExecuteSql(
                                @"SELECT NUMERIC_PRECISION, NUMERIC_SCALE --, *
                                    FROM INFORMATION_SCHEMA.COLUMNS
                                    WHERE TABLE_SCHEMA = 'dbo'
		                                    AND TABLE_NAME = @prm0   
		                                    AND COLUMN_NAME = @prm1
                                 ", className, fieldname);
                            if (decimalColumn != null && decimalColumn.Rows.Count == 1)
                            {
                                int scale = (int)decimalColumn.Rows[0]["NUMERIC_SCALE"];
                                int precision = (int)(byte)decimalColumn.Rows[0]["NUMERIC_PRECISION"];
                                if (scale != definition.Scale || precision != definition.Precision)
                                {
                                    string scalechangeSql = "ALTER TABLE " + className + " ALTER COLUMN " + fieldname + " " + MapType(definition);
                                    if (run)
                                        transaction.ExecuteNonQuery(scalechangeSql);

                                    str.Append("-- Alter Field: " + fieldname + ", Table: " + className + Environment.NewLine);
                                    str.Append(scalechangeSql + ";" + Environment.NewLine + Environment.NewLine);
                                }
                            }
                        }
                    }
                    foreach (DataColumn column in table.Columns)
                    {
                        if (!fieldDictionary.ContainsKey(column.ColumnName))
                        {
                            string dropKey = "--alter table " + className +  " drop constraint [FK_"  + className +  "_" + column.ColumnName + "]";
                            string sql = "-- alter table " + className + " drop column " + column.ColumnName;
                            if (column.ColumnName.EndsWith("_Id"))
                                sql = dropKey + Environment.NewLine + sql;

                            if (run)
                                transaction.ExecuteNonQuery(sql);

                            str.Append("-- Drop Field: " + column.ColumnName + ", Table: " + className + Environment.NewLine);
                            str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                        }
                    }
                }
                else
                {
                    //Hiç özelliği olmayan class lar olabilir
                    if (props.Length == 0)
                    {
                        continue;
                    }

                    StringBuilder s = new System.Text.StringBuilder();
                    s.Append("CREATE TABLE " + className + " (");
                    foreach (PropertyInfo property in props)
                    {
                        bool isEntityPointer = property.PropertyType.IsGenericType
                            && property.PropertyType.GetGenericTypeDefinition() == typeof(SharpPointer<>);

                        FieldDefinitionAttribute definition = DataDictionary.Instance.GetFieldDefinition(entityType, property);
                        string fieldname = (isEntityPointer || property.PropertyType.IsSubclassOf(baseEntityType))
                            ? definition.Name + "_Id"
                            : definition.Name;

                        var propType = property.PropertyType.IsGenericType ? property.PropertyType.GetGenericArguments()[0] : property.PropertyType;

                        s.Append(fieldname + " " + MapType(definition));
                        if (definition.Name == "Id")
                        {
                            s.Append(" IDENTITY(1,1) CONSTRAINT PK_" + className + "_Id PRIMARY KEY CLUSTERED");
                        }
                        s.Append(", ");

                        if (fieldname.EndsWith("_Id"))
                        {
                            string fkSql = string.Format(@"ALTER TABLE {0} ADD CONSTRAINT
                                	FK_{0}_{2} FOREIGN KEY
	                                ({2}) REFERENCES {1}(Id) ON UPDATE  NO ACTION ON DELETE  NO ACTION", className, propType.Name, fieldname);

                            keys.Append(fkSql + Environment.NewLine + Environment.NewLine);
                        }
                    }
                    s.Remove(s.Length - 2, 2);
                    s.Append(")");

                    string sql = s.ToString() + Environment.NewLine;
                    if (run)
                        transaction.ExecuteNonQuery(sql);
                    
                    str.Append("-- New Table: " + className + Environment.NewLine);
                    str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                }

                System.Collections.Hashtable fields = new System.Collections.Hashtable();
                Dictionary<string, List<string>> uniqueGroup = new Dictionary<string, List<string>>();
                Dictionary<string, List<string>> nonUniqueGroup = new Dictionary<string, List<string>>();
                foreach (PropertyInfo property in props)
                {
                    FieldDefinitionAttribute definition = DataDictionary.Instance.GetFieldDefinition(entityType, property);
                    string fieldname = (property.PropertyType.IsSubclassOf(baseEntityType) || property.PropertyType.IsGenericType) ?
                        definition.Name + "_Id" : definition.Name;

                    string indexPrefix = "IX_" + className;
                    string indexName = !string.IsNullOrEmpty(definition.UniqueIndexGroup) 
                        ? definition.UniqueIndexGroup : (definition.NonUniqueIndexGroup ?? "");
                    indexName = indexName.Replace("$", indexPrefix);

                    if (!string.IsNullOrEmpty(definition.UniqueIndexGroup))
                    {
                        if (!uniqueGroup.ContainsKey(indexName))
                            uniqueGroup[indexName] = new List<string>();
                        
                        uniqueGroup[indexName].Add(fieldname);                        
                    }
                    if (!string.IsNullOrEmpty(definition.NonUniqueIndexGroup))
                    {
                        if (!nonUniqueGroup.ContainsKey(indexName))
                            nonUniqueGroup[indexName] = new List<string>();

                        nonUniqueGroup[indexName].Add(fieldname);
                    }
                }
                foreach (string indexGroup in uniqueGroup.Keys)
                {
                    //Base class'lar içindeki alanların index adları için $ kullanılmalı
                    string indexPrefix = "IX_" + className;
                    string indexName = indexGroup;
                    indexName = indexName.Replace("$", indexPrefix);

                    string sql = CreateIndex(className, true, indexName, uniqueGroup[indexGroup], transaction);
                    if (string.IsNullOrEmpty(sql)) continue;
                    
                    if (run)
                        transaction.ExecuteNonQuery(sql);

                    str.Append("-- Index: " + indexGroup + Environment.NewLine);
                    str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                }
                foreach (string indexGroup in nonUniqueGroup.Keys)
                {
                    //Base class'lar içindeki alanların index adları için $ kullanılmalı
                    string indexPrefix = "IX_" + className;
                    string indexName = indexGroup;
                    indexName = indexName.Replace("$", indexPrefix);

                    string sql = CreateIndex(className, false, indexName, nonUniqueGroup[indexGroup], transaction);
                    if (string.IsNullOrEmpty(sql)) continue;

                    if (run)
                        transaction.ExecuteNonQuery(sql);

                    str.Append("-- Index Nonunique: " + indexGroup + Environment.NewLine);
                    str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                }
            }

            string keysSql = keys.ToString();
            if (!string.IsNullOrEmpty(keysSql))
            {
                if (run)
                {
                    transaction.ExecuteNonQuery(keysSql);
                }
                str.Append("-- FK's: " + Environment.NewLine);
                str.Append(keysSql + ";" + Environment.NewLine + Environment.NewLine);
            }
        }

        private static string CreateIndex(string className, bool unique, string indexGroup, List<string> fields, Transaction transaction)
        {
            /* HATAAAAAAAA ne alaka yazılmış anlaşılmıyor. yoksa oluşturmuyor, yoksa oluşturmak lazım!!!
            if (transaction.ExecuteSql("SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[" + className + "]') AND name = N'" + indexGroup + "'")
                .Rows.Count > 0)
            {
                return "";
            }*/

            string sqlScript = "";
            sqlScript += System.Environment.NewLine;
            sqlScript += " IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[" + className + "]') AND name = N'" + indexGroup + "')";
            sqlScript += System.Environment.NewLine;
            sqlScript += " BEGIN";
            sqlScript += System.Environment.NewLine;
            sqlScript += " CREATE ";
            if (unique)
            {
                sqlScript += " UNIQUE ";
            }
            sqlScript += " NONCLUSTERED ";
            
            sqlScript += " INDEX [" + indexGroup + "] ON [dbo].[" + className + "] ";

            sqlScript += " (";
            sqlScript += System.Environment.NewLine;
            
            foreach (var field in fields)
            {  
                sqlScript += "     [" + field.Trim() + "] ASC,";
                sqlScript += System.Environment.NewLine;
            }           

            sqlScript = sqlScript.Substring(0, sqlScript.Length - ("," + System.Environment.NewLine).Length);
            sqlScript += System.Environment.NewLine;
            sqlScript += " )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]";
            sqlScript += System.Environment.NewLine;
            sqlScript += " END";

            return sqlScript;
        }

        private static void updateMysql(StringBuilder str, Assembly assembly, Type baseEntityType, bool run, Transaction transaction)
        {        
            foreach (Type entityType in assembly.GetTypes())
            {
                if (!entityType.IsClass
                    || !entityType.IsSubclassOf(baseEntityType)
                    || entityType.IsGenericType
                    || entityType.Name.Contains("<")) continue;

                var np = entityType.GetCustomAttributes(typeof(NonPersistentAttribute), false);
                if (np != null && np.Length > 0) continue;

                PropertyInfo[] props =
                    PersistenceStrategyProvider.FindStrategyFor(entityType)
                    .GetPersistentProperties(entityType);

                string className = entityType.Name;
                if (TableExistMy(className, transaction.GetSchema(), transaction))
                {
                    DataTable table = transaction.ExecuteSql("select * from " + className + " where Id=-1");
                    DataTable metaTable = transaction.MetaTableColumns(className);
                    Dictionary<string, int> metaLengths = new Dictionary<string, int>();
                    foreach (DataRow row in metaTable.Rows)
                    {
                        if ((string)row["DATA_TYPE"] != "varchar") continue;
                        metaLengths[(string)row["COLUMN_NAME"]] = Convert.ToInt32(row["CHARACTER_MAXIMUM_LENGTH"]);
                    }

                    System.Collections.Hashtable fieldDictionary = new System.Collections.Hashtable();
                    foreach (PropertyInfo property in props)
                    {
                        if (!property.CanWrite) continue;

                        FieldDefinitionAttribute definition = DataDictionary.Instance.GetFieldDefinition(entityType, property);
                        string fieldname = (property.PropertyType.IsSubclassOf(baseEntityType) || property.PropertyType.IsGenericType) ?
                            definition.Name + "_Id" : definition.Name;

                        fieldDictionary[fieldname] = 1;
                        if (!table.Columns.Contains(fieldname))
                        {
                            string sql = "alter table " + className + " add " + fieldname + " " + MapTypeMysql(definition);
                            if (run)
                                transaction.ExecuteNonQuery(sql);

                            str.Append("-- New Field: " + fieldname + ", Table: " + className + Environment.NewLine);
                            str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                        }
                        else if (!fieldname.EndsWith("_Id")
                            && definition.TypeName != "Boolean"
                            && table.Columns[fieldname].DataType.Name != definition.TypeName)
                        {
                            if (definition.TypeName.ToLowerInvariant() == "text"
                                && table.Columns[fieldname].DataType.Name.ToLowerInvariant() == "string") continue;

                            string sql = "alter table " + className + " modify column " + fieldname + " " + MapTypeMysql(definition);
                            if (run)
                                transaction.ExecuteNonQuery(sql);

                            str.Append("-- Alter Field: " + fieldname + ", Table: " + className + Environment.NewLine);
                            str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                        }
                        else if (!fieldname.EndsWith("_Id")
                            && definition.TypeName == "Boolean"
                            && table.Columns[fieldname].DataType.Name != "Boolean") //table.Columns[fieldname].DataType.Name != "SByte"
                        {
                            string sql = "alter table " + className + " modify column " + fieldname + " " + MapTypeMysql(definition);
                            if (run)
                                transaction.ExecuteNonQuery(sql);

                            str.Append("-- Alter Field: " + fieldname + ", Table: " + className + Environment.NewLine);
                            str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                        }
                    }
                    foreach (DataColumn column in table.Columns)
                    {
                        if (!fieldDictionary.ContainsKey(column.ColumnName))
                        {
                            string sql = "-- alter table " + className + " drop column " + column.ColumnName;
                            if (run)
                                transaction.ExecuteNonQuery(sql);

                            str.Append("-- Drop Field: " + column.ColumnName + ", Table: " + className + Environment.NewLine);
                            str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                        }
                    }
                }
                else
                {
                    //Hiç özelliği olmayan class lar olabilir
                    if (props.Length == 0)
                    {
                        continue;
                    }

                    StringBuilder s = new System.Text.StringBuilder();
                    s.Append("CREATE TABLE " + className + " (");
                    string pkstr = "";
                    foreach (PropertyInfo property in props)
                    {
                        FieldDefinitionAttribute definition = DataDictionary.Instance.GetFieldDefinition(entityType, property);
                        string fieldname = (property.PropertyType.IsSubclassOf(baseEntityType)) ?
                            definition.Name + "_Id" : definition.Name;
                        s.Append("`" + fieldname + "` " + MapTypeMysql(definition));
                        if (definition.Name == "Id")
                        {

                            s.Replace("DEFAULT 0", "").Append(" NOT NULL AUTO_INCREMENT");
                            pkstr = " ,PRIMARY KEY (`Id`)";
                        }

                        s.Append(", ");
                    }
                    s.Remove(s.Length - 2, 2);
                    s.Append(pkstr);
                    s.Append(")");

                    string sql = s.ToString();
                    if (run)
                        transaction.ExecuteNonQuery(sql);

                    str.Append("-- New Table: " + className + Environment.NewLine);
                    str.Append(sql + ";" + Environment.NewLine + Environment.NewLine);
                }
            }
        }

        private static string MapType(FieldDefinitionAttribute definition)
        {
            switch (definition.TypeName)
            {
                case "Byte":
                    return "tinyint";
                case "String":
                    return "nvarchar(" + (definition.Length > 0 ? "" + definition.Length : "Max") + ")";
                case "Int32":
                    return "int";
                case "Int64":
                    return "bigint";
                case "Boolean":
                    return "bit";
                case "Decimal":
                    return "decimal(" + definition.Precision + "," + definition.Scale + ")";
                case "Double":
                    return "real";
                case "DateTime":
                    return "DateTime";
                case "Text":
                    return "ntext";
                case "Byte[]":
                    return "varbinary(" + (definition.Length > 0 ? "" + definition.Length : "Max") + ")";
                default:
                    return "int"; //id field of foregin keys.
            }
        }

        private static string MapTypeMysql(FieldDefinitionAttribute definition)
        {
            switch (definition.TypeName)
            {
                case "Byte":
                    return "tinyint unsigned DEFAULT 0";
                case "String":
                    return "nvarchar(" + definition.Length + ")";
                case "Int32":
                    return "int  DEFAULT 0";
                case "Boolean":
                    return "tinyint(1) DEFAULT 0";
                case "Decimal":
                    return "decimal(18,4) DEFAULT 0";
                case "DateTime":
                    return "DateTime";
                case "Text":
                    return "text";
                default:
                    return "int"; //id field of foregin keys.
            }
        }

        private static bool TableExistMs(string tableName, Transaction transaction)
        {
            return
                1 == transaction.ExecuteScalarI(
                        @"SELECT COUNT(*) AS tablecount
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE (TABLE_TYPE = 'BASE TABLE') AND (TABLE_NAME = '" + tableName + "')",
                        null);
        }

        private static bool TableExistMy(string tableName, string schema, Transaction transaction)
        {
            return
                1 == transaction.ExecuteScalarI(
                        @"SELECT count(*) 
                        FROM information_schema.tables 
                        WHERE                                                     
                            table_name = '" + tableName + "' and TABLE_SCHEMA='" + schema + "'");
        }       
    }
}
