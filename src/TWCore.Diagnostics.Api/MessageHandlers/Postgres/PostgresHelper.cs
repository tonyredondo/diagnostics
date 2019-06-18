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

        public static async Task<Dictionary<string, List<object>>> ExecuteReaderAsync(string commandText, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            var columnValues = new Dictionary<string, List<object>>();
            await ExecuteReaderAsync(command => FillCommand(command, commandText, parameters), async (reader, token) =>
            {
                var columns = new string[reader.FieldCount];
                for (var i = 0; i < columns.Length; i++)
                {
                    columns[i] = reader.GetName(i);
                    columnValues[columns[i]] = new List<object>();
                }

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    for(var i = 0; i < columns.Length; i++)
                        columnValues[columns[i]].Add(reader.GetValue(i));
                }

            }, cancellationToken).ConfigureAwait(false);
            return columnValues;
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

        public class PostgresSettings : SettingsBase
        {
            public string ConnectionString { get; set; }
        }
    }
}
