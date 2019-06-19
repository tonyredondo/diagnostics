using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TWCore.Settings;
using TWCore.Serialization;

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres
{
    public static class PostgresHelper
    {
        private static PostgresSettings Settings = Core.GetSettings<PostgresSettings>();

        static PostgresHelper()
        {
            NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
        }

        public static async Task<int> ExecuteNonQueryAsync(Action<NpgsqlCommand> prepareCommand, CancellationToken cancellationToken = default)
        {
            using (var connection = new NpgsqlConnection(Settings.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var command = connection.CreateCommand())
                {
                    prepareCommand(command);
                    return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
        public static async Task<object> ExecuteScalarAsync(Action<NpgsqlCommand> prepareCommand, CancellationToken cancellationToken = default)
        {
            using (var connection = new NpgsqlConnection(Settings.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var command = connection.CreateCommand())
                {
                    prepareCommand(command);
                    return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
        public static async Task ExecuteReaderAsync(Action<NpgsqlCommand> prepareCommand, Func<DbDataReader, CancellationToken, Task> dataHandler, CancellationToken cancellationToken = default)
        {
            using (var connection = new NpgsqlConnection(Settings.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var command = connection.CreateCommand())
                {
                    prepareCommand(command);
                    var dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    await dataHandler(dataReader, cancellationToken).ConfigureAwait(false);
                }
            }
        }


        public static Task<int> ExecuteNonQueryAsync(string commandText, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
            => ExecuteNonQueryAsync(command => FillCommand(command, commandText, parameters), cancellationToken);

        public static Task<object> ExecuteScalarAsync(string commandText, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
            => ExecuteScalarAsync(command => FillCommand(command, commandText, parameters), cancellationToken);

        public static async Task<DbResult> ExecuteReaderAsync(string commandText, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            var dbResult = new DbResult();
            await ExecuteReaderAsync(command => FillCommand(command, commandText, parameters), async (reader, token) =>
            {
                var columns = new string[reader.FieldCount];
                var dictioPattern = new Dictionary<string, object>();
                var totalCountIdx = -1;
                for (var i = 0; i < columns.Length; i++)
                {
                    var cName = reader.GetName(i);
                    if (cName == "_query_totalcount")
                    {
                        totalCountIdx = i;
                    }
                    else
                    {
                        columns[i] = cName;
                        dictioPattern[cName] = null;
                    }
                }

                var totalCount = 0;
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var row = new DbRow(dictioPattern);
                    for (var i = 0; i < columns.Length; i++)
                    {
                        if (totalCountIdx == i)
                        {
                            if (totalCount == 0)
                                totalCount = reader.GetInt32(i);
                        }
                        else
                        {
                            row[columns[i]] = reader.GetValue(i);
                        }
                    }
                    dbResult.Add(row);
                }
                if (totalCount == 0)
                    totalCount = dbResult.Count;

                dbResult.TotalCount = totalCount;
            }, cancellationToken).ConfigureAwait(false);
            return dbResult;
        }

        public class DbResult : List<DbRow>
        {
            public int TotalCount { get; set; }
        }

        public class DbRow : Dictionary<string, object>
        {
            public DbRow(IDictionary<string, object> dictio) : base(dictio) {}
        }

        private static void FillCommand(NpgsqlCommand command, string commandText, IDictionary<string, object> parameters)
        {
            command.CommandText = commandText;
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var itemParam in parameters)
                {
                    var paramValue = itemParam.Value;
                    if (paramValue?.GetType().IsEnum == true)
                        paramValue = (int)paramValue;
                    if (paramValue is SerializableException serEx)
                        command.Parameters.Add(new NpgsqlParameter(itemParam.Key, NpgsqlTypes.NpgsqlDbType.Jsonb)
                        {
                            Value = serEx
                        });
                    else
                        command.Parameters.Add(new NpgsqlParameter(itemParam.Key, paramValue ?? DBNull.Value));
                }
            }
        }

        private class PostgresSettings : SettingsBase
        {
            public string ConnectionString { get; set; }
        }
    }
}
