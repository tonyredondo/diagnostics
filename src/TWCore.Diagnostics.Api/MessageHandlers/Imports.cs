using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TWCore.Diagnostics.Api.MessageHandlers.Postgres;
using TWCore.Diagnostics.Api.MessageHandlers.RavenDb;
using TWCore.Diagnostics.Api.Models.Counters;
using TWCore.Diagnostics.Api.Models.Log;
using TWCore.Diagnostics.Api.Models.Trace;
using TWCore.Diagnostics.Log;

namespace TWCore.Diagnostics.Api.MessageHandlers
{
    public static class Imports
    {
        public static Task ImportLogsAsync()
        {
            return RavenHelper.ExecuteAsync(async session =>
            {
                var pDal = new PostgresDal();

                var query = session.Advanced.AsyncDocumentQuery<NodeLogItem>();
                var index = 0;
                var enumerator = await session.Advanced.StreamAsync(query).ConfigureAwait(false);

                var insertBuffer = new List<Postgres.Entities.EntLog>();
                Core.Log.InfoBasic("Importing logs...");
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var item = enumerator.Current.Document;
                    index++;
                    if (index % 1000 == 0)
                        Core.Log.InfoBasic("Writing: " + index);

                    insertBuffer.Add(new Postgres.Entities.EntLog
                    {
                        LogId = item.LogId,
                        Environment = item.Environment,
                        Machine = item.Machine,
                        Application = item.Application,
                        Assembly = item.Assembly,
                        Type = item.Type,
                        Code = item.Code,
                        Group = item.Group,
                        Level = item.Level,
                        Timestamp = item.Timestamp,
                        Message = item.Message,
                        Exception = item.Exception
                    });

                    if (insertBuffer.Count == 500)
                    {
                        Core.Log.InfoBasic("Saving...");
                        try
                        {
                            await pDal.InsertLogAsync(insertBuffer, true).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Core.Log.Write(ex);
                        }
                        insertBuffer.Clear();
                    }
                }
                if (insertBuffer.Count > 0)
                {
                    try
                    {
                        await pDal.InsertLogAsync(insertBuffer, true).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Write(ex);
                    }
                    insertBuffer.Clear();
                }
                Core.Log.InfoBasic("Total Items: " + index);
            });
        }

        public static Task ImportTracesAsync()
        {
            return RavenHelper.ExecuteAsync(async session =>
            {
                var pDal = new PostgresDal();

                var query = session.Advanced.AsyncDocumentQuery<NodeTraceItem>();
                var index = 0;
                var enumerator = await session.Advanced.StreamAsync(query).ConfigureAwait(false);

                var insertBuffer = new List<Postgres.Entities.EntTrace>();
                Core.Log.InfoBasic("Importing traces...");
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var item = enumerator.Current.Document;
                    index++;
                    if (index % 1000 == 0)
                        Core.Log.InfoBasic("Writing: " + index);

                    insertBuffer.Add(new Postgres.Entities.EntTrace
                    {
                        TraceId = item.TraceId,
                        Environment = item.Environment,
                        Application = item.Application,
                        Machine = item.Machine,
                        Group = item.Group,
                        Formats = item.Formats,
                        Name = item.Name,
                        Tags = item.Tags,
                        Timestamp = item.Timestamp
                    });

                    if (insertBuffer.Count == 500)
                    {
                        Core.Log.InfoBasic("Saving...");
                        try
                        {
                            await pDal.InsertTraceAsync(insertBuffer, true).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Core.Log.Write(ex);
                        }
                        insertBuffer.Clear();
                    }
                }
                if (insertBuffer.Count > 0)
                {
                    try
                    {
                        await pDal.InsertTraceAsync(insertBuffer, true).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Write(ex);
                    }
                    insertBuffer.Clear();
                }
                Core.Log.InfoBasic("Total Items: " + index);
            });
        }

        public static Task ImportMetadataAsync()
        {
            return RavenHelper.ExecuteAsync(async session =>
            {
                var pDal = new PostgresDal();

                var query = session.Advanced.AsyncDocumentQuery<GroupMetadata>();
                var index = 0;
                var enumerator = await session.Advanced.StreamAsync(query).ConfigureAwait(false);

                var insertBuffer = new List<Postgres.Entities.EntMeta>();
                Core.Log.InfoBasic("Importing metadata...");
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var item = enumerator.Current.Document;
                    index++;
                    if (index % 1000 == 0)
                        Core.Log.InfoBasic("Writing: " + index);

                    if (item.Items != null)
                    {
                        foreach (var kv in item.Items)
                        {
                            insertBuffer.Add(new Postgres.Entities.EntMeta
                            {
                                Environment = null,
                                Group = item.GroupName,
                                Timestamp = item.Timestamp,
                                Key = kv.Key,
                                Value = kv.Value
                            });
                        }
                    }

                    if (insertBuffer.Count >= 500)
                    {
                        Core.Log.InfoBasic("Saving...");
                        try
                        {
                            await pDal.InsertMetadataAsync(insertBuffer).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Core.Log.Write(ex);
                        }
                        insertBuffer.Clear();
                    }
                }
                if (insertBuffer.Count > 0)
                {
                    try
                    {
                        await pDal.InsertMetadataAsync(insertBuffer).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Write(ex);
                    }
                    insertBuffer.Clear();
                }
                Core.Log.InfoBasic("Total Items: " + index);
            });
        }

        public static Task ImportCounterAsync()
        {
            return RavenHelper.ExecuteAsync(async session =>
            {
                var pDal = new PostgresDal();

                var query = session.Advanced.AsyncDocumentQuery<NodeCountersItem>();
                var index = 0;
                var enumerator = await session.Advanced.StreamAsync(query).ConfigureAwait(false);

                var insertBuffer = new List<Postgres.Entities.EntCounter>();
                Core.Log.InfoBasic("Importing counters...");
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var item = enumerator.Current.Document;
                    index++;
                    if (index % 1000 == 0)
                        Core.Log.InfoBasic("Writing: " + index);

                    insertBuffer.Add(new Postgres.Entities.EntCounter
                    {
                        CounterId = item.CountersId,
                        Environment = item.Environment,
                        Application = item.Application,
                        Category = item.Category,
                        Kind = item.Kind,
                        Level = item.Level,
                        Name = item.Name,
                        Type = item.Type,
                        Unit = item.Unit,
                        TypeOfValue = item.TypeOfValue
                    });

                    if (insertBuffer.Count >= 500)
                    {
                        Core.Log.InfoBasic("Saving...");
                        try
                        {
                            await pDal.InsertCounterAsync(insertBuffer, true).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Core.Log.Write(ex);
                        }
                        insertBuffer.Clear();
                    }
                }
                if (insertBuffer.Count > 0)
                {
                    try
                    {
                        await pDal.InsertCounterAsync(insertBuffer, true).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Write(ex);
                    }
                    insertBuffer.Clear();
                }
                Core.Log.InfoBasic("Total Items: " + index);
            });
        }

        public static Task ImportCounterValuesAsync()
        {
            return RavenHelper.ExecuteAsync(async session =>
            {
                var pDal = new PostgresDal();

                var query = session.Advanced.AsyncDocumentQuery<NodeCountersValue>();
                var index = 0;
                var enumerator = await session.Advanced.StreamAsync(query).ConfigureAwait(false);

                var insertBuffer = new List<Postgres.Entities.EntCounterValue>();
                Core.Log.InfoBasic("Importing counters values...");
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var item = enumerator.Current.Document;
                    index++;
                    if (index % 1000 == 0)
                        Core.Log.InfoBasic("Writing: " + index);

                    insertBuffer.Add(new Postgres.Entities.EntCounterValue
                    {
                        CounterId = item.CountersId,
                        Timestamp = item.Timestamp,
                        Value = Convert.ToDouble(item.Value)
                    });

                    if (insertBuffer.Count >= 500)
                    {
                        Core.Log.InfoBasic("Saving...");
                        try
                        {
                            await pDal.InsertCounterValueAsync(insertBuffer, true).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Core.Log.Write(ex);
                        }
                        insertBuffer.Clear();
                    }
                }
                if (insertBuffer.Count > 0)
                {
                    try
                    {
                        await pDal.InsertCounterValueAsync(insertBuffer, true).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Write(ex);
                    }
                    insertBuffer.Clear();
                }
                Core.Log.InfoBasic("Total Items: " + index);
            });
        }
    }
}
