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
            var response = 0;
            using (var connection = new NpgsqlConnection(Settings.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                //connection.Open();
                using (var command = connection.CreateCommand())
                {
                    prepareCommand(command);
                    try
                    {
                        response = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch(Exception ex)
                    {
                        Core.Log.Write(ex);
                        connection.Close();
                        await Task.Delay(5000).ConfigureAwait(false);
                        connection.Open();
                        response = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            return response;
        }
        public static async Task<object> ExecuteScalarAsync(Action<NpgsqlCommand> prepareCommand, CancellationToken cancellationToken = default)
        {
            using (var connection = new NpgsqlConnection(Settings.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                //connection.Open();
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
                //connection.Open();
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
            using (var connection = new NpgsqlConnection(Settings.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                //connection.Open();
                using (var command = connection.CreateCommand())
                {
                    FillCommand(command, commandText, parameters);
                    var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    
                    var columns = new string[reader.FieldCount];
                    var totalCountIdx = -1;
                    var totalCount = 0;

                    for (var i = 0; i < columns.Length; i++)
                    {
                        var cName = reader.GetName(i);
                        if (cName == "_query_totalcount")
                            totalCountIdx = i;
                        else
                            columns[i] = cName;
                    }

                    if (totalCountIdx == -1)
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var values = new object[columns.Length];
                            reader.GetValues(values);
                            dbResult.Add(new DbRow(columns, values));
                        }
                    }
                    else
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var row = new DbRow(columns);
                            for (var i = 0; i < columns.Length; i++)
                            {
                                if (totalCountIdx == i)
                                {
                                    if (totalCount == 0)
                                        totalCount = reader.GetInt32(i);
                                }
                                else
                                {
                                    row[i] = reader.GetValue(i);
                                }
                            }
                            dbResult.Add(row);
                        }
                    }

                    if (totalCount == 0)
                        totalCount = dbResult.Count;

                    dbResult.TotalCount = totalCount;
                }
            }
            return dbResult;
        }

        public class DbResult : List<DbRow>
        {
            public int TotalCount { get; set; }
        }

        public readonly struct DbRow
        {
            private readonly string[] _headers;
            private readonly object[] _values;

            public DbRow(string[] headers)
            {
                _headers = headers;
                _values = new object[_headers.Length];
            }

            public DbRow(string[] header, object[] values)
            {
                _headers = header;
                _values = values;
            }

            public object this[int i]
            {
                get => _values[i];
                set => _values[i] = value;
            }

            public object this[string key]
            {
                get
                {
                    for (var i = 0; i < _headers.Length; i++)
                    {
                        if (string.Equals(_headers[i], key, StringComparison.Ordinal))
                            return _values[i];
                    }
                    throw new IndexOutOfRangeException();
                }
                set
                {
                    for (var i = 0; i < _headers.Length; i++)
                    {
                        if (!string.Equals(_headers[i], key, StringComparison.Ordinal)) continue;
                        _values[i] = value;
                        return;
                    }
                    throw new IndexOutOfRangeException();
                }
            }
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
