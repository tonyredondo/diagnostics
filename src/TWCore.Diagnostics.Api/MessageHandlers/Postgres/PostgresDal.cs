using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TWCore.Diagnostics.Api.MessageHandlers.Postgres.Entities;

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

        public Task<Dictionary<string, List<object>>> GetAllLogsAsync()
        {
            return PostgresHelper.ExecuteReaderAsync("SELECT * FROM logs");
        }
    }
}
