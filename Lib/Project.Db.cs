using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace TaskUI.Lib
{
    using Net.Bndy;
    using Net.Bndy.Data;
    using Net.Bndy.Data.SqlServer;
    public abstract partial class Project
    {
        /// <summary>
        /// Gets the safe SQL.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <returns>System.String.</returns>
        protected string GetSafeSql(string sql)
        {
            return sql.Replace("'", "''");
        }
        /// <summary>
        /// Checks whether the specified rows exist in database.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="condition">The condition.</param>
        /// <returns><c>true</c> if existed, <c>false</c> otherwise.</returns>
        protected bool ExistsInDatabase(string tableName, string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                condition = "1=1";

            bool result = false;
            var sql = string.Format(@"SELECT * FROM [{0}] WHERE {1}", tableName, condition);

        again:
            try
            {
                result = _dbFactory.ExistsRows(sql);
            }
            catch (SqlException ex)
            {
                OnError(ex, new { Sql = sql }, ActionTypeOnError.Retry);
                goto again;
            }
            return result;
        }
        /// <summary>
        /// Gets the value of first row and first column.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="field">The field.</param>
        /// <param name="condition">The condition.</param>
        /// <returns>The value of first row and first column</returns>
        protected object GetScalarValueFromDatabase(string tableName, string field, string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                condition = "1=1";

            object result = null;
            var sql = string.Format(@"SELECT {0} FROM [{1}] WHERE {2}",
                    field, tableName, condition);
        again:
            try
            {
                result = _dbFactory.ExecuteScalar(sql);
            }
            catch (SqlException ex)
            {
                OnError(ex, new { Sql = sql }, ActionTypeOnError.Retry);
                goto again;
            }
            return result == DBNull.Value ? null : result;
        }
        /// <summary>
        /// Gets the value of first row and first column.
        /// </summary>
        /// <param name="tableName">The name of table</param>
        /// <param name="field">The field for returning</param>
        /// <param name="condition">The condition</param>
        /// <param name="server">The server of database</param>
        /// <param name="db">The name of database</param>
        /// <param name="user">The user for logging into database</param>
        /// <param name="pwd">The password for logging into database</param>
        /// <returns></returns>
        protected object GetScalarValueFromDatabase(string tableName, string field, string condition,
            string server, string db, string user, string pwd)
        {
            using (var df = new MsSqlFactory(server, db, user, pwd))
            {
                if (string.IsNullOrWhiteSpace(condition))
                    condition = "1=1";

                object result = null;
                var sql = string.Format(@"SELECT {0} FROM [{1}] WHERE {2}",
                        field, tableName, condition);
            again:
                try
                {
                    result = df.ExecuteScalar(sql);
                }
                catch (SqlException ex)
                {
                    OnError(ex, new { Sql = sql }, ActionTypeOnError.Retry);
                    goto again;
                }
                return result == DBNull.Value ? null : result;
            }
        }
        /// <summary>
        /// Queries the database.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fields">The fields to query.</param>
        /// <param name="condition">The condition.</param>
        /// <param name="orderBy">The order by.</param>
        /// <param name="top">The top.</param>
        /// <returns>The rows</returns>
        protected List<Dictionary<string, object>> QueryFromDatabase(string tableName,
            string fields, string condition, string orderBy = null, int? top = null)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
            if (string.IsNullOrWhiteSpace(fields))
                fields = "*";
            if (string.IsNullOrWhiteSpace(condition))
                condition = "1=1";
            if (!string.IsNullOrWhiteSpace(orderBy))
                orderBy = "ORDER BY " + orderBy;

            var sTop = "";
            if (top.HasValue)
                sTop = "TOP " + top.Value;

            DataTable dt;
            var sql = string.Format(@"SELECT {4} {0} FROM [{1}] WHERE {2} {3}",
                    fields, tableName, condition, orderBy, sTop);
        again:
            try
            {
                dt = _dbFactory.Query(sql);
            }
            catch (SqlException ex)
            {
                OnError(ex, new { Sql = sql }, ActionTypeOnError.Retry);
                goto again;
            }

            foreach (DataRow dr in dt.Rows)
            {
                Dictionary<string, object> row = new Dictionary<string, object>();
                foreach (DataColumn dc in dt.Columns)
                {
                    if (dr[dc.ColumnName] != DBNull.Value)
                        row.Add(dc.ColumnName, dr[dc.ColumnName]);
                    else
                        row.Add(dc.ColumnName, null);
                }
                result.Add(row);
            }
            return result;
        }
        /// <summary>
        /// Imports data to database.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="data">The data.</param>
        /// <param name="idFormat">The field name or format, example: subject, {Title} - {Section}</param>
        /// <returns>The identity</returns>
        protected object Import2Database(string tableName,
            Dictionary<string, object> data, string idFormat, [CallerMemberName] string methodName = "")
        {
            var id = idFormat;
            if (!string.IsNullOrWhiteSpace(idFormat))
            {
                if (Regex.IsMatch(idFormat, @"{\w+}"))
                    id = data.Format(id);
                else if (data.ContainsKey(id) && data[id] != null)
                    id = data[id].ToString();

            }

            if (this.EnableScriptGenerate)
            {
                var excludedColumns = data.Keys.Where(__ => __.StartsWith("_")).ToList();
                excludedColumns.Insert(0, tableName);
                var sql = data.ToInsertSql(excludedColumns.ToArray()).Replace("[TableName]", tableName)
                    + Environment.NewLine;
                File.AppendAllText(GetScriptFile(SqlScriptType.Insert), sql);
            }

            if (!this.EnableDatabase)
            {
                OnActionChanged(new ActionChangedEventArgs(
                    this,
                    string.Format("Skip {0}.", id),
                    ActionType.Skipped));
                return null;
            }

            object result = null;
        again:
            try
            {
                OnActionChanged(new ActionChangedEventArgs(
                    this,
                    string.Format("Importing {0} to [{1}]...", id, tableName),
                    ActionType.Save2Database,
                    methodName));

                result = this._dbFactory.Insert(tableName, data);

                CountDbInserts(tableName, result, data);

                StartInfo.AffectedRows++;
            }
            catch (SqlException ex)
            {
                OnError(ex, data, ActionTypeOnError.Retry);
                goto again;
            }

            return result == DBNull.Value ? null : result;
        }


        /// <summary>
        /// Executes the SQL.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        protected void ExecuteSql(string sql)
        {
            _dbFactory.ExecuteNonQuery(sql);
        }

        /// <summary>
        /// Imports data to database.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="data">The anonymous object includes data.</param>
        /// <param name="idFormat">The field name or format, example: subject, {Title} - {Section}</param>
        /// <returns>System.Object.</returns>
        protected object Import2Database(string tableName, object data,
            string idFormat = null, string[] excludedColumns = null, [CallerMemberName] string methodName = "")
        {
            var dic = data.ToDict();
            Dictionary<string, object> d = new Dictionary<string, object>();
            foreach (KeyValuePair<string, string> kv in dic)
            {
                if (excludedColumns != null
                    && excludedColumns.Contains(kv.Key))
                    continue;

                if (kv.Value != null)
                {
                    if (idFormat == null) idFormat = kv.Key;
                    d.Add(kv.Key, kv.Value);
                }
            }

            return Import2Database(tableName, d, idFormat, methodName);
        }
        /// <summary>
        /// Updates data in database.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="conditionOrFormat">The condition or condition format.</param>
        /// <param name="data">The data.</param>
        protected void UpdateFromDatabase(string tableName, string conditionOrFormat, object data, [CallerMemberName] string methodName = "")
        {
            var condition = conditionOrFormat;

            Dictionary<string, object> d = new Dictionary<string, object>();
            foreach (KeyValuePair<string, string> kv in data.ToDict())
            {
                d.Add(kv.Key, kv.Value);
                condition = condition.Replace(string.Format("{{{0}}}", kv.Key), kv.Value ?? "");
            }

            if (this.EnableScriptGenerate)
            {
                var excludedColumns = d.Keys.Where(__ => __.StartsWith("_")).ToList();

                var sql = d.ToUpdateSql(condition, excludedColumns.ToArray()).Replace("[TableName]", tableName)
                    + Environment.NewLine;
                File.AppendAllText(GetScriptFile(SqlScriptType.Update), sql);
            }

            OnActionChanged(new ActionChangedEventArgs(
                this,
                string.Format("Update database [{0}] - {1}...", tableName, condition),
                ActionType.UpdateDatabase,
                methodName)
            );

        again:
            try
            {

                _dbFactory.Update(tableName, d, condition);

                CountDbUpdates(tableName, condition);
            }
            catch (SqlException ex)
            {
                OnError(ex, data, ActionTypeOnError.Retry);
                goto again;
            }
        }
        /// <summary>
        /// Deletes from database.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="condition">The condition.</param>
        /// <returns>System.Int32.</returns>
        protected int DeleteFromDatabase(string tableName, string condition)
        {
            if (this.EnableScriptGenerate)
            {
                var sql = string.Format(
                    "DELETE FROM [{0}] WHERE {1}{2}",
                    tableName,
                    condition ?? "1=0",
                    Environment.NewLine);
                File.AppendAllText(GetScriptFile(SqlScriptType.Delete), sql);
            }

            int result = 0;

        again:
            try
            {
                CountDbDeletes(tableName, condition);

                result = _dbFactory.Delete(tableName, condition);
            }
            catch (SqlException ex)
            {
                OnError(ex, new { Table = tableName, Condition = condition }, ActionTypeOnError.Retry);
                goto again;
            }

            return result;
        }
        /// <summary>
        /// Gets unique identifier. Insert the data to database If does not exist,
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="idColumnName">Name of the unique identifier column.</param>
        /// <param name="conditionFormat">The condition format. like name = '{Name}'</param>
        /// <param name="data">The data.</param>
        /// <param name="isNew">Return true if new, otherwise false</param>
        /// <returns>System.Object.</returns>
        protected object ForceGetID(string tableName, string idColumnName,
            string conditionFormat, object data, out bool isNew)
        {
            var condition = conditionFormat;
            foreach (KeyValuePair<string, string> d in data.ToDict())
            {
                condition = condition.Replace(string.Format("{{{0}}}", d.Key), d.Value ?? "");
            }

            object id = null;

            if (!ExistsInDatabase(tableName, condition))
            {
                Import2Database(tableName, data);
                isNew = true;
            }
            else
            {
                isNew = false;
            }
            id = GetScalarValueFromDatabase(tableName, idColumnName, condition);

            return id;
        }
        /// <summary>
        /// Gets the top N rows.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="condition">The condition.</param>
        /// <param name="orderby">The orderby.</param>
        /// <returns>List{Dictionary{System.StringSystem.Object}}.</returns>
        protected List<Dictionary<string, object>> GetTopNRows(int n, string tableName, string condition, string orderby)
        {
            return QueryFromDatabase(tableName, "*", condition, orderby, n);
        }
        /// <summary>
        /// Gets unique identifier. Insert the data to database If does not exist, 
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="idColumnName">Name of the unique identifier column.</param>
        /// <param name="conditionFormat">The condition format. like name = '{Name}'</param>
        /// <param name="data">The data.</param>
        /// <returns>System.Object.</returns>
        protected object ForceGetID(string tableName, string idColumnName,
            string conditionFormat, object data)
        {
            bool isNew;
            return ForceGetID(tableName, idColumnName, conditionFormat, data, out isNew);
        }
        /// <summary>
        /// Back up database
        /// </summary>
        protected void BackupDatabase()
        {
            var sql = string.Format(@"BACKUP DATABASE [{0}] TO DISK='{1}' WITH NAME = 'DataCollector-AutoBackup-v{2}'",
                this.DbName,
                Path.Combine(this.DataVersionDir, string.Format("{0}_{1}_AutoBackup.bak",
                    this.DbName, DateTime.Now.ToString("yyyy-MM-dd_HHmmss"))),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        again:
            try
            {
                _dbFactory.ExecuteNonQuery(sql);
            }
            catch (SqlException ex)
            {
                OnError(ex, new { Sql = sql }, ActionTypeOnError.Retry);
                goto again;
            }
        }
    }
}
