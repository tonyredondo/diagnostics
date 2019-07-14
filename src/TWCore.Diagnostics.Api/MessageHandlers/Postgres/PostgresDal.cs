using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TWCore.Diagnostics.Api.Models.Database.Postgres.Entities;
using TWCore.Diagnostics.Log;
using TWCore.Serialization;

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres
{
    public class PostgresDal
    {
        private static readonly CultureInfo CultureFormat = CultureInfo.GetCultureInfo("en-US");
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new ConcurrentDictionary<Type, PropertyInfo[]>();

        #region Inserts
        public Task<int> InsertLogAsync(params EntLog[] logItems) => InsertLogAsync((IEnumerable<EntLog>)logItems);
        public Task<int> InsertTraceAsync(params EntTrace[] traceItems) => InsertTraceAsync((IEnumerable<EntTrace>)traceItems);
        public Task<int> InsertMetadataAsync(params EntMeta[] metaItems) => InsertMetadataAsync((IEnumerable<EntMeta>)metaItems);
        public Task<int> InsertCounterAsync(params EntCounter[] counterItems) => InsertCounterAsync((IEnumerable<EntCounter>)counterItems);
        public Task<int> InsertCounterValueAsync(params EntCounterValue[] counterValueItems) => InsertCounterValueAsync((IEnumerable<EntCounterValue>)counterValueItems);
        public Task<int> InsertStatusAsync(params EntStatus[] statusItem) => InsertStatusAsync((IEnumerable<EntStatus>)statusItem);
        public Task<int> InsertStatusValuesAsync(params EntStatusValue[] statusValueItem) => InsertStatusValuesAsync((IEnumerable<EntStatusValue>)statusValueItem);


        public async Task<int> InsertLogAsync(IEnumerable<EntLog> logItems, bool ignoreConflict = false)
        {
            const string ValuesPattern = "SELECT @LogId, @Environment, @Machine, @Application, @Timestamp, @Assembly, @Type, @Group, @Code, @Level, @Message, @Exception, @Date ";

            var query = "INSERT INTO logs (log_id, environment, machine, application, timestamp, assembly, type, \"group\", code, level, message, exception, date) \n";
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
            const string ValuesPattern = "SELECT @Group, @Environment, @Timestamp, @Key, @Value, @Date ";

            var query = "INSERT INTO metadata (\"group\", environment, timestamp, key, value, date)  \n";
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
        public async Task<int> InsertStatusAsync(IEnumerable<EntStatus> statusItem, bool ignoreConflict = false)
        {
            const string ValuesPattern = "SELECT @StatusId, @Environment, @Machine, @Application, @Timestamp, @ApplicationDisplay, @Elapsed, @StartTime, @Date ";

            var query = "INSERT INTO status (status_id, environment, machine, application, timestamp, application_display, elapsed, start_time, date) \n";
            var lstValues = new List<string>();
            foreach (var item in statusItem)
            {
                lstValues.Add(ReplaceInPattern(ValuesPattern, item));
            }
            query += string.Join("UNION ALL \n", lstValues) + "\n";
            if (ignoreConflict)
                query += "ON CONFLICT (status_id) DO NOTHING;";
            try
            {
                return await PostgresHelper.ExecuteNonQueryAsync(query).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception("Error on Query: \n" + query, ex);
            }
        }
        public async Task<int> InsertStatusValuesAsync(IEnumerable<EntStatusValue> statusValueItem, bool ignoreConflict = false)
        {
            const string ValuesPattern = "SELECT @StatusId, @Key, @Value, @Type ";

            var query = "INSERT INTO status_values (status_id, key, value, type) \n";
            var lstValues = new List<string>();
            foreach (var item in statusValueItem)
            {
                lstValues.Add(ReplaceInPattern(ValuesPattern, item));
            }
            query += string.Join("UNION ALL \n", lstValues) + "\n";
            if (ignoreConflict)
                query += "ON CONFLICT (status_id, key) DO NOTHING;";
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

        #region Logs
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
        public Task<PostgresHelper.DbResult> GetLogsByApplication(string environment, string application, DateTime fromDate, DateTime toDate, int page, int pageSize)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetLogsByApplication2.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@Application"] = application,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate,
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
        #endregion

        #region Traces

        public Task<PostgresHelper.DbResult> GetTracesByEnvironment(string environment, DateTime fromDate, DateTime toDate, int page, int pageSize)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetTracesByEnvironment.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate,
                ["@Page"] = page,
                ["@PageSize"] = pageSize
            });
        }
        public Task<PostgresHelper.DbResult> GetTracesByEnvironmentWithErrors(string environment, DateTime fromDate, DateTime toDate, int page, int pageSize)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetTracesByEnvironmentWithErrors.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate,
                ["@Page"] = page,
                ["@PageSize"] = pageSize
            });
        }

        public Task<PostgresHelper.DbResult> GetTracesByGroupId(string environment, string group)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetTracesByGroupId.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@Group"] = group,
            });
        }

        public Task<PostgresHelper.DbResult> GetTracesByTraceId(Guid traceId)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetTracesByTraceId.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@TraceId"] = traceId
            });
        }


        #endregion

        #region Metadata

        public Task<PostgresHelper.DbResult> GetMetadataByGroup(string group)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetMetadataByGroup.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Group"] = group,
            });
        }

        public Task<PostgresHelper.DbResult> SearchMetadata(string search, DateTime fromDate, DateTime toDate)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.SearchMetadata.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Search"] = search,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate
            });
        }

        #endregion

        #region Groups
        public Task<PostgresHelper.DbResult> GetGroupsByEnvironment(string environment, DateTime fromDate, DateTime toDate, int page, int pageSize)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetGroupsByEnvironment.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate,
                ["@Page"] = page,
                ["@PageSize"] = pageSize
            });
        }
        public Task<PostgresHelper.DbResult> GetGroupsByEnvironmentWithErrors(string environment, DateTime fromDate, DateTime toDate, int page, int pageSize)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetGroupsByEnvironmentWithErrors.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate,
                ["@Page"] = page,
                ["@PageSize"] = pageSize
            });
        }
        #endregion

        #region Search

        public Task<PostgresHelper.DbResult> Search(string environment, string search, DateTime fromDate, DateTime toDate, int limit)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.SearchGroup.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@Search"] = search,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate,
                ["@Limit"] = limit
            });
        }
        public Task<PostgresHelper.DbResult> SearchExact(string environment, string search, DateTime fromDate, DateTime toDate, int limit)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.SearchGroupExact.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@Search"] = search,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate,
                ["@Limit"] = limit
            });
        }

        #endregion

        #region Counters

        public Task<PostgresHelper.DbResult> GetCounters(string environment)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetCounters.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment
            });
        }

        public Task<PostgresHelper.DbResult> GetCounter(Guid counterId)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetCounterById.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@CounterId"] = counterId
            });
        }

        public Task<PostgresHelper.DbResult> GetCounter(string environment, string application, string category, string name)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetCounter.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@Application"] = application,
                ["@Category"] = category,
                ["@Name"] = name
            });
        }

        public Task<PostgresHelper.DbResult> GetCountersValues(Guid counterId, DateTime fromDate, DateTime toDate)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetCountersValues.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@CounterId"] = counterId,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate
            });
        }

        #endregion

        #region Statuses

        public Task<PostgresHelper.DbResult> GetStatus(Guid statusId)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetStatusById.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@@StatusId"] = statusId,
            });
        }

        public Task<PostgresHelper.DbResult> DeleteStatus(Guid statusId)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.DeleteStatus.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@StatusId"] = statusId,
            });
        }

        public Task<PostgresHelper.DbResult> GetStatuses(string environment, string machine = null, string application = null)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetStatuses.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@Machine"] = machine,
                ["@Application"] = application
            });
        }

        public Task<PostgresHelper.DbResult> GetStatuses(string environment, string machine, string application, DateTime fromDate, DateTime toDate, int page, int pageSize)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetStatusesPaged.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Environment"] = environment,
                ["@Machine"] = machine,
                ["@Application"] = application,
                ["@FromDate"] = fromDate,
                ["@ToDate"] = toDate,
                ["@Page"] = page,
                ["@PageSize"] = pageSize
            });
        }


        public Task<PostgresHelper.DbResult> GetStatusesValues(Guid[] ids)
        {
            var query = typeof(PostgresDal).Assembly.GetResourceString("Postgres.Sql.GetStatusesValues.sql");
            return PostgresHelper.ExecuteReaderAsync(query, new Dictionary<string, object>
            {
                ["@Ids"] = ids
            });
        }

        #endregion
    }
}
