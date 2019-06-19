using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TWCore.Diagnostics.Api.MessageHandlers.Postgres.Entities;
using TWCore.Diagnostics.Log;

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres
{
    public class PostgresDal
    {

        public Task<int> InsertLogAsync(EntLog logItem)
        {
            var query = "INSERT INTO logs (log_id, environment, machine, application, timestamp, assembly, type, \"group\", code, level, message, exception) " +
                "VALUES (@LogId, @Environment, @Machine, @Application, @Timestamp, @Assembly, @Type, @Group, @Code, @Level, @Message, @Exception)";
            return PostgresHelper.ExecuteNonQueryAsync(query, logItem.ToDictionary());
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
