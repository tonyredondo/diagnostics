using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TWCore.Diagnostics.Api.MessageHandlers.Postgres.Entities;
using TWCore.Diagnostics.Log;
using TWCore.Serialization;

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres
{
    public class PostgresDal
    {
        private static readonly CultureInfo CultureFormat = CultureInfo.GetCultureInfo("en-US");
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new ConcurrentDictionary<Type, PropertyInfo[]>();

        #region Inserts
        public Task<int> InsertLogAsync(params EntLog[] logItems)
            => InsertLogAsync((IEnumerable<EntLog>)logItems);
        public Task<int> InsertTraceAsync(params EntTrace[] traceItems)
           => InsertTraceAsync((IEnumerable<EntTrace>)traceItems);
        public Task<int> InsertMetadataAsync(params EntMeta[] metaItems)
           => InsertMetadataAsync((IEnumerable<EntMeta>)metaItems);

        public async Task<int> InsertLogAsync(IEnumerable<EntLog> logItems, bool ignoreConflict = false)
        {
            const string ValuesPattern = "SELECT @LogId, @Environment, @Machine, @Application, @Timestamp, @Assembly, @Type, @Group, @Code, @Level, @Message, @Exception ";

            var query = "INSERT INTO logs (log_id, environment, machine, application, timestamp, assembly, type, \"group\", code, level, message, exception) \n";
            var lstValues = new List<string>();
            foreach (var item in logItems)
            {
                lstValues.Add(ReplaceInPattern(ValuesPattern, item));
            }
            query += string.Join("UNION ALL \n", lstValues) + "\n";
            if (ignoreConflict)
                query += "ON CONFLICT (log_id) DO NOTHING;";

            try
            {
                return await PostgresHelper.ExecuteNonQueryAsync(query).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                throw new Exception("Error on Query: \n" + query, ex);
            }
        }
        public async Task<int> InsertTraceAsync(IEnumerable<EntTrace> traceItems, bool ignoreConflict = false)
        {
            const string ValuesPattern = "SELECT @TraceId, @Environment, @Machine, @Application, @Timestamp, @Tags, @Group, @Name, @Formats ";

            var query = "INSERT INTO traces (trace_id, environment, machine, application, timestamp, tags, \"group\", name, formats) \n";
            var lstValues = new List<string>();
            foreach (var item in traceItems)
            {
                lstValues.Add(ReplaceInPattern(ValuesPattern, item));
            }
            query += string.Join("UNION ALL \n", lstValues) + "\n";
            if (ignoreConflict)
                query += "ON CONFLICT (trace_id) DO NOTHING;";

            try
            {
                return await PostgresHelper.ExecuteNonQueryAsync(query).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception("Error on Query: \n" + query, ex);
            }
        }
        public async Task<int> InsertMetadataAsync(IEnumerable<EntMeta> metaItems)
        {
            const string ValuesPattern = "SELECT @Group, @Environment, @Timestamp, @Key, @Value ";

            var query = "INSERT INTO metadata (\"group\", environment, timestamp, key, value)  \n";
            var lstValues = new List<string>();
            foreach (var item in metaItems)
            {
                lstValues.Add(ReplaceInPattern(ValuesPattern, item));
            }
            query += string.Join("UNION ALL \n", lstValues) + "\n";

            try
            {
                return await PostgresHelper.ExecuteNonQueryAsync(query).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception("Error on Query: \n" + query, ex);
            }
        }
        public async Task<int> InsertCounterAsync(IEnumerable<EntCounter> counterItems, bool ignoreConflict = false)
        {
            const string ValuesPattern = "SELECT @CounterId, @Environment, @Application, @Category, @Name, @Type, @Level, @Kind, @Unit, @TypeOfValue ";

            var query = "INSERT INTO counters (counter_id, environment, application, category, name, type, level, kind, unit, typeofvalue) \n";
            var lstValues = new List<string>();
            foreach (var item in counterItems)
            {
                lstValues.Add(ReplaceInPattern(ValuesPattern, item));
            }
            query += string.Join("UNION ALL \n", lstValues) + "\n";
            if (ignoreConflict)
                query += "ON CONFLICT (counter_id) DO NOTHING;";
            try
            {
                return await PostgresHelper.ExecuteNonQueryAsync(query).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception("Error on Query: \n" + query, ex);
            }
        }
        public async Task<int> InsertCounterValueAsync(IEnumerable<EntCounterValue> counterValueItems, bool ignoreConflict = false)
        {
            const string ValuesPattern = "SELECT @CounterId, @Timestamp, @Value ";

            var query = "INSERT INTO counters_values (counter_id, timestamp, value) \n";
            var lstValues = new List<string>();
            foreach (var item in counterValueItems)
            {
                lstValues.Add(ReplaceInPattern(ValuesPattern, item));
            }
            query += string.Join("UNION ALL \n", lstValues) + "\n";
            if (ignoreConflict)
                query += "ON CONFLICT (counter_id, timestamp) DO NOTHING;";
            try
            {
                return await PostgresHelper.ExecuteNonQueryAsync(query).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception("Error on Query: \n" + query, ex);
            }
        }


        private static string ReplaceInPattern(string pattern, object item)
        {
            const string NullDateTime = "null::timestamp";
            const string NullTimeSpan = "null::time";
            const string NullGuid = "null::uuid";
            const string NullSerializableException = "null::json";

            var itemType = item.GetType();
            var properties = Properties.GetOrAdd(itemType, type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty));
            foreach(var prop in properties)
            {
                var key = "@" + prop.Name;
                var value = prop.GetValue(item);
                if (value is null)
                {
                    var propType = prop.PropertyType.GetUnderlyingType();

                    if (propType == typeof(DateTime))
                        value = NullDateTime;
                    else if (propType == typeof(TimeSpan))
                        value = NullTimeSpan;
                    else if (propType == typeof(Guid))
                        value = NullGuid;
                    else if (propType == typeof(SerializableException))
                        value = NullSerializableException;
                    else
                        value = "null";
                }
                else if (value is DateTime dateValue)
                {
                    value = "'" + dateValue.ToString("O") + "'::timestamp";
                }
                else if (value is TimeSpan timeValue)
                {
                    value = "'" + timeValue.ToString("c") + "'::time";
                }
                else if (value is Guid guidValue)
                {
                    value = "'" + guidValue.ToString("D") + "'::uuid";
                }
                else if (value is string strValue)
                {
                    value = "'" + strValue.Replace("'", "''") + "'";
                }
                else if (value is SerializableException serEx)
                {
                    value = "'" + serEx.SerializeToJson().Replace("'", "''") + "'::json";
                }
                else if (value.GetType().IsEnum == true)
                {
                    value = (int)value;
                }
                else if (value is string[] strArray)
                {
                    value = "ARRAY [" + string.Join(", ", strArray.Select(i => "'" + i.Replace("'", "''") + "'")) + "]";
                }
                else if (value is int[] intArray)
                {
                    value = "ARRAY [" + string.Join(", ", intArray.Select(i => i.ToString(CultureFormat))) + "]";
                }
                else if (value is IConvertible convValue)
                {
                    value = convValue.ToString(CultureFormat);
                }
                pattern = pattern.Replace(key, value.ToString());
            }
            return pattern;
        }
        #endregion

        public Task<PostgresHelper.DbResult> GetAllEnvironments()
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetAllEnvironments.sql");
            return PostgresHelper.ExecuteReaderAsync(query);
        }

        public Task<PostgresHelper.DbResult> GetLogLevelsByEnvironment(string environment, DateTime fromDate, DateTime toDate)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetLogLevelsByEnvironment.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate
            });
        }
        
        public Task<PostgresHelper.DbResult> GetLogsByApplication(string environment, string application, LogLevel logLevel, DateTime fromDate, DateTime toDate, int page, int pageSize)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetLogsByApplication.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@Application"] = application,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate,
                ["@LogLevel"] = (int)logLevel,
                ["@Page"] = page,
                ["@PageSize"] = pageSize
            });
        }
        
        public Task<PostgresHelper.DbResult> GetLogsByGroup(string environment, string group, DateTime fromDate, DateTime toDate)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetLogsByGroup.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@Group"] = group,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate
            });
        }
        
        public Task<PostgresHelper.DbResult> SearchLogs(string environment, string search, DateTime fromDate, DateTime toDate)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.SearchLogs.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@Search"] = search,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate
            });
        }
    }
}
