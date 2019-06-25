using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TWCore.Diagnostics.Api.MessageHandlers.Postgres;
using TWCore.Diagnostics.Api.MessageHandlers.RavenDb;
using TWCore.Diagnostics.Api.Models.Counters;
using TWCore.Diagnostics.Api.Models.Log;
using TWCore.Diagnostics.Api.Models.Status;
using TWCore.Diagnostics.Api.Models.Trace;
using TWCore.Diagnostics.Log;

namespace TWCore.Diagnostics.Api.MessageHandlers
{
    public static class Imports
    {
        private static readonly CultureInfo CultureFormat = CultureInfo.GetCultureInfo("en-US");

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
                        Exception = item.Exception,
                        Date = item.Timestamp.Date
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
                                Date = item.Timestamp.Date,
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


        public static Task ImportStatusesAsync()
        {
            return RavenHelper.ExecuteAsync(async session =>
            {
                var pDal = new PostgresDal();

                var query = session.Advanced.AsyncDocumentQuery<NodeStatusItem>();
                var index = 0;
                var enumerator = await session.Advanced.StreamAsync(query).ConfigureAwait(false);

                var insertBuffer = new List<Postgres.Entities.EntStatus>();
                var insertBuffer2 = new List<Postgres.Entities.EntStatusValue>();
                Core.Log.InfoBasic("Importing statuses...");
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var item = enumerator.Current.Document;
                    index++;
                    if (index % 10 == 0)
                        Core.Log.InfoBasic("Writing: " + index);

                    var status = new Postgres.Entities.EntStatus
                    {
                        Application = item.Application,
                        ApplicationDisplay = item.ApplicationDisplayName,
                        Date = item.Date,
                        Elapsed = item.ElapsedMilliseconds,
                        Environment = item.Environment,
                        Machine = item.Machine,
                        StartTime = item.StartTime,
                        StatusId = item.InstanceId,
                        Timestamp = item.Timestamp
                    };
                    insertBuffer.Add(status);

                    if (item.Values != null)
                    {
                        foreach(var value in item.Values)
                        {
                            insertBuffer2.Add(new Postgres.Entities.EntStatusValue
                            {
                                StatusId = status.StatusId,
                                Type = value.Type,
                                Key = value.Key,
                                Value = value.Value is IConvertible vConv ? vConv.ToString(CultureFormat) : value.Value?.ToString()
                            });
                        }
                    }

                    if (insertBuffer.Count >= 10 || insertBuffer2.Count >= 200)
                    {
                        Core.Log.InfoBasic("Saving...");
                        try
                        {
                            await pDal.InsertStatusAsync(insertBuffer, true).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Core.Log.Write(ex);
                        }
                        insertBuffer.Clear();
                        foreach (var innerBuffer in insertBuffer2.Batch(500))
                        {
                            try
                            {
                                await pDal.InsertStatusValuesAsync(innerBuffer, true).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Core.Log.Write(ex);
                            }
                        }
                        insertBuffer2.Clear();
                    }
                }
                if (insertBuffer.Count > 0 || insertBuffer2.Count > 0)
                {
                    Core.Log.InfoBasic("Saving...");
                    try
                    {
                        await pDal.InsertStatusAsync(insertBuffer, true).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Write(ex);
                    }
                    insertBuffer.Clear();
                    foreach (var innerBuffer in insertBuffer2.Batch(500))
                    {
                        try
                        {
                            await pDal.InsertStatusValuesAsync(innerBuffer, true).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Core.Log.Write(ex);
                        }
                    }
                    insertBuffer2.Clear();
                }
                Core.Log.InfoBasic("Total Items: " + index);
            });
        }
    }
}
