using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TWCore.Diagnostics.Api.MessageHandlers.Postgres.Entities;
using TWCore.Diagnostics.Log;
using TWCore.Serialization;

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres
{
    public class PostgresDal
    {
        private static CultureInfo CultureFormat = CultureInfo.GetCultureInfo("en-US");

        public Task<int> InsertLogAsync(params EntLog[] logItems)
            => InsertLogAsync((IEnumerable<EntLog>)logItems);

        public async Task<int> InsertLogAsync(IEnumerable<EntLog> logItems)
        {
            const string ValuesPattern = "SELECT @LogId, @Environment, @Machine, @Application, @Timestamp, @Assembly, @Type, @Group, @Code, @Level, @Message, @Exception ";
            //const string ValuesPattern = "(@LogId, @Environment, @Machine, @Application, @Timestamp, @Assembly, @Type, @Group, @Code, @Level, @Message, @Exception)";

            //var query = "INSERT INTO logs (log_id, environment, machine, application, timestamp, assembly, type, \"group\", code, level, message, exception) VALUES \n";
            var query = "INSERT INTO logs (log_id, environment, machine, application, timestamp, assembly, type, \"group\", code, level, message, exception) \n";
            var lstValues = new List<string>();
            foreach (var item in logItems)
            {
                lstValues.Add(ReplaceInPattern(ValuesPattern, item));
            }
            //query += string.Join(", \n", lstValues);
            query += string.Join("UNION ALL \n", lstValues) + "\n";
            query += "ON CONFLICT (log_id) DO NOTHING;";

            try
            {
                return await PostgresHelper.ExecuteNonQueryAsync(query);
            }
            catch(Exception ex)
            {
                throw new Exception("Error on Query: \n" + query, ex);
            }
        }
        private static string ReplaceInPattern(string pattern, object item)
        {
            var properties = item.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.GetProperty);
            foreach(var prop in properties)
            {
                var key = "@" + prop.Name;
                var value = prop.GetValue(item);
                if (value is null)
                {
                    value = "null";
                    var propType = prop.PropertyType.GetUnderlyingType();

                    if (propType == typeof(DateTime))
                        value += "::timestamp";
                    else if (propType == typeof(TimeSpan))
                        value += "::time";
                    else if (propType == typeof(Guid))
                        value += "::uuid";
                    else if (propType == typeof(SerializableException))
                        value += "::json";
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
                else if (value is IConvertible convValue)
                {
                    value = convValue.ToString(CultureFormat);
                }
                pattern = pattern.Replace(key, value.ToString());
            }
            return pattern;
        }


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
