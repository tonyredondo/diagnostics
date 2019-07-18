/*
Copyright 2015-2018 Daniel Adrian Redondo Suarez

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWCore.Compression;
using TWCore.Diagnostics.Api.Models;
using TWCore.Diagnostics.Api.Models.Database.Postgres.Entities;
using TWCore.Diagnostics.Api.Models.Status;
using TWCore.Diagnostics.Counters;
using TWCore.Diagnostics.Log;
using TWCore.Diagnostics.Status;
using TWCore.Diagnostics.Trace.Storages;
using TWCore.IO;
using TWCore.Messaging;
using TWCore.Serialization;
using TWCore.Serialization.NSerializer;

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres
{
    public class PostgresMessagesHandler : IDiagnosticMessagesHandler
    {
        private static readonly CultureInfo CultureFormat = CultureInfo.GetCultureInfo("en-US");
        private static readonly DiagnosticsSettings Settings = Core.GetSettings<DiagnosticsSettings>();
        private static readonly ICompressor Compressor = new GZipCompressor();
        private static readonly NBinarySerializer NBinarySerializer = new NBinarySerializer
        {
            Compressor = Compressor
        };
        private static readonly XmlTextSerializer XmlSerializer = new XmlTextSerializer
        {
            Compressor = Compressor
        };
        private static readonly JsonTextSerializer JsonSerializer = new JsonTextSerializer
        {
            Compressor = Compressor,
            Indent = true,
            EnumsAsStrings = true,
            UseCamelCase = true
        };
        private static readonly PostgresDal Dal = new PostgresDal();

        /// <inheritdoc />
        public void Init()
        {
        }

        /// <inheritdoc />
        public async Task ProcessLogItemsMessageAsync(List<LogItem> message)
        {
            if (message is null) return;
            using (Watch.Create($"Processing LogItems List Message [{message.Count} items]", LogLevel.InfoBasic))
            {
                try
                {
                    var logs = new List<EntLog>();
                    foreach (var logItem in message)
                    {
                        var item = new EntLog
                        {
                            Application = logItem.ApplicationName,
                            Assembly = logItem.AssemblyName,
                            Code = logItem.Code,
                            Date = logItem.Timestamp.Date,
                            Environment = logItem.EnvironmentName,
                            Exception = logItem.Exception,
                            Group = logItem.GroupName,
                            Level = logItem.Level,
                            LogId = logItem.Id,
                            Machine = logItem.MachineName,
                            Message = logItem.Message,
                            Timestamp = logItem.Timestamp,
                            Type = logItem.TypeName
                        };
                        logs.Add(item);
                    }
                    foreach(var batch in logs.Batch(500))
                        await Dal.InsertLogAsync(batch, true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
        }

        /// <inheritdoc />
        public async Task ProcessGroupMetadataMessageAsync(List<GroupMetadata> message)
        {
            if (message is null) return;
            using (Watch.Create($"Processing GroupMetadata List Message [{message.Count} items]", LogLevel.InfoBasic))
            {
                try
                {
                    var metadatas = new List<EntMeta>();
                    foreach (var groupMetaItem in message)
                    {
                        if (groupMetaItem.Items == null) continue;
                        foreach (var metaValue in groupMetaItem.Items)
                        {
                            var item = new EntMeta
                            {
                                Date = groupMetaItem.Timestamp.Date,
                                Environment = groupMetaItem.EnvironmentName,
                                Group = groupMetaItem.GroupName,
                                Timestamp = groupMetaItem.Timestamp,
                                Key = metaValue.Key,
                                Value = metaValue.Value
                            };
                            metadatas.Add(item);
                        }
                    }

                    foreach(var batch in metadatas.Batch(500))
                        await Dal.InsertMetadataAsync(batch).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
        }

        /// <inheritdoc />
        public async Task ProcessTraceItemsMessageAsync(List<MessagingTraceItem> message)
        {
            if (message is null) return;
            using (Watch.Create($"Processing TraceItems List Message [{message.Count} items]", LogLevel.InfoBasic))
            {
                foreach (var traceItem in message)
                {
                    try
                    {
                        var item = new EntTrace
                        {
                            Application = traceItem.ApplicationName,
                            Environment = traceItem.EnvironmentName,
                            Group = traceItem.GroupName,
                            Machine = traceItem.MachineName,
                            Name = traceItem.TraceName,
                            Tags = traceItem.Tags?.Select(i => i.ToString()).Join(", "),
                            Timestamp = traceItem.Timestamp,
                            TraceId = traceItem.Id
                        };

                        if (traceItem.TraceObject != null)
                        {
                            var lstExtensions = new List<string>();
                            using (var msNBinary = new RecycleMemoryStream())
                            using (var msXml = new RecycleMemoryStream())
                            using (var msJson = new RecycleMemoryStream())
                            using (var msTxt = new RecycleMemoryStream())
                            {

                                var writeBinary = Settings.WriteInBinary || Settings.ForceBinaryOnApp.Contains(traceItem.ApplicationName, StringComparer.OrdinalIgnoreCase);
                                if (writeBinary)
                                {
                                    #region NBinary
                                    try
                                    {
                                        NBinarySerializer.Serialize(traceItem.TraceObject, msNBinary);
                                        msNBinary.Position = 0;
                                        await TraceDiskStorage.StoreAsync(item, msNBinary, ".nbin.gz").ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Core.Log.Write(ex);
                                    }
                                    #endregion
                                }

                                var writeInXml = Settings.WriteInXml || Settings.ForceXmlOnApp.Contains(traceItem.ApplicationName, StringComparer.OrdinalIgnoreCase);
                                if (writeInXml)
                                {
                                    #region Xml Serializer
                                    try
                                    {
                                        var bXml = false;
                                        if (traceItem.TraceObject is SerializedObject serObj)
                                        {
                                            var value = serObj.GetValue();
                                            switch (value)
                                            {
                                                case ResponseMessage rsMessage when rsMessage?.Body != null:
                                                    var bodyValueRS = rsMessage.Body.GetValue();
                                                    if (!(bodyValueRS is IDictionary))
                                                    {
                                                        XmlSerializer.Serialize(bodyValueRS, msXml);
                                                        bXml = true;
                                                    }
                                                    break;
                                                case RequestMessage rqMessage when rqMessage?.Body != null:
                                                    var bodyValueRQ = rqMessage.Body.GetValue();
                                                    if (!(bodyValueRQ is IDictionary))
                                                    {
                                                        XmlSerializer.Serialize(bodyValueRQ, msXml);
                                                        bXml = true;
                                                    }
                                                    break;
                                                default:
                                                    if (value != null && value.GetType() != typeof(string))
                                                    {
                                                        if (!(value is IDictionary))
                                                        {
                                                            XmlSerializer.Serialize(value, msXml);
                                                            bXml = true;
                                                        }
                                                    }
                                                    break;
                                            }
                                        }
                                        else if (!(traceItem.TraceObject is string) && !(traceItem.TraceObject is IDictionary))
                                        {
                                            XmlSerializer.Serialize(traceItem.TraceObject, msXml);
                                            bXml = true;
                                        }

                                        if (bXml)
                                        {
                                            msXml.Position = 0;
                                            await TraceDiskStorage.StoreAsync(item, msXml, ".xml.gz").ConfigureAwait(false);
                                            lstExtensions.Add("XML");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Core.Log.Write(ex);
                                    }
                                    #endregion
                                }

                                var writeInJson = Settings.WriteInJson || Settings.ForceJsonOnApp.Contains(traceItem.ApplicationName, StringComparer.OrdinalIgnoreCase);
                                if (writeInJson)
                                {
                                    #region Json Serializer
                                    try
                                    {
                                        var bJson = false;
                                        if (traceItem.TraceObject is SerializedObject serObj)
                                        {
                                            var value = serObj.GetValue();
                                            switch (value)
                                            {
                                                case ResponseMessage rsMessage when rsMessage?.Body != null:
                                                    JsonSerializer.Serialize(rsMessage.Body.GetValue(), msJson);
                                                    bJson = true;
                                                    break;
                                                case RequestMessage rqMessage when rqMessage?.Body != null:
                                                    JsonSerializer.Serialize(rqMessage.Body.GetValue(), msJson);
                                                    bJson = true;
                                                    break;
                                                default:
                                                    if (value != null && value.GetType() != typeof(string))
                                                    {
                                                        JsonSerializer.Serialize(value, msJson);
                                                        bJson = true;
                                                    }
                                                    break;
                                            }
                                        }
                                        else if (!(traceItem.TraceObject is string))
                                        {
                                            JsonSerializer.Serialize(traceItem.TraceObject, msJson);
                                            bJson = true;
                                        }

                                        if (bJson)
                                        {
                                            msJson.Position = 0;
                                            await TraceDiskStorage.StoreAsync(item, msJson, ".json.gz").ConfigureAwait(false);
                                            lstExtensions.Add("JSON");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Core.Log.Write(ex);
                                    }
                                    #endregion
                                }

                                #region String Serializer
                                try
                                {
                                    var bTxt = false;
                                    var strValue = string.Empty;
                                    if (traceItem.TraceObject is SerializedObject serObj)
                                    {
                                        var value = serObj.GetValue();
                                        if (value is string txtValue)
                                        {
                                            strValue = txtValue;
                                            msTxt.Write(Encoding.UTF8.GetBytes(txtValue).ToGzip().AsReadOnlySpan());
                                            bTxt = true;
                                        }
                                    }
                                    else if (traceItem.TraceObject is string strObj)
                                    {
                                        strValue = strObj;
                                        msTxt.Write(Encoding.UTF8.GetBytes(strObj).ToGzip().AsReadOnlySpan());
                                        bTxt = true;
                                    }

                                    if (bTxt)
                                    {
                                        msTxt.Position = 0;

                                        if (strValue.IsValidJson())
                                        {
                                            await TraceDiskStorage.StoreAsync(item, msTxt, ".json.gz").ConfigureAwait(false);
                                            lstExtensions.Add("JSON");
                                        }
                                        else if (strValue.IsValidXml())
                                        {
                                            await TraceDiskStorage.StoreAsync(item, msTxt, ".xml.gz").ConfigureAwait(false);
                                            lstExtensions.Add("XML");
                                        }
                                        else
                                        {
                                            await TraceDiskStorage.StoreAsync(item, msTxt, ".txt.gz").ConfigureAwait(false);
                                            lstExtensions.Add("TXT");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Core.Log.Write(ex);
                                }
                                #endregion

                                if (lstExtensions.Count > 0)
                                {
                                    item.Formats = lstExtensions.ToArray();
                                    await Dal.InsertTraceAsync(item);
                                }
                            }
                        }
                        else
                        {
                            await Dal.InsertTraceAsync(item);
                        }
                    }
                    catch(Exception ex)
                    {
                        Core.Log.Write(ex);
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task ProcessCountersMessageAsync(List<ICounterItem> message)
        {
            if (message is null) return;
            using (var watch = Watch.Create($"Processing Counter item List Message [{message.Count} items]", LogLevel.InfoBasic))
            {
                try
                {
                    var lstCounters = new List<EntCounterValue>();

                    foreach (var counter in message)
                    {
                        EntCounter cEntity = null;

                        var results = await Dal.GetCounter(counter.Environment, counter.Application, counter.Category, counter.Name);
                        if (results.Count > 0)
                        {
                            cEntity = PostgresBindings.GetCounterEntity(results[0]);
                        }
                        else
                        {
                            cEntity = new EntCounter
                            {
                                Environment = counter.Environment,
                                Application = counter.Application,
                                CounterId = Guid.NewGuid(),
                                Category = counter.Category,
                                Name = counter.Name,
                                Level = counter.Level,
                                Kind = counter.Kind,
                                Unit = counter.Unit,
                                Type = counter.Type,
                                TypeOfValue = counter.TypeOfValue.Name
                            };
                            await Dal.InsertCounterAsync(new[] { cEntity }, true).ConfigureAwait(false);
                        }

                        if (counter is CounterItem<int> intCounter)
                        {
                            foreach (var value in intCounter.Values)
                            {
                                var nValue = new EntCounterValue
                                {
                                    CounterId = cEntity.CounterId,
                                    Timestamp = value.Timestamp,
                                    Value = value.Value
                                };
                                lstCounters.Add(nValue);
                            }
                        }
                        else if (counter is CounterItem<double> doubleCounter)
                        {
                            foreach (var value in doubleCounter.Values)
                            {
                                var nValue = new EntCounterValue
                                {
                                    CounterId = cEntity.CounterId,
                                    Timestamp = value.Timestamp,
                                    Value = value.Value
                                };
                                lstCounters.Add(nValue);
                            }
                        }
                        else if (counter is CounterItem<decimal> decimalCounter)
                        {
                            foreach (var value in decimalCounter.Values)
                            {
                                var nValue = new EntCounterValue
                                {
                                    CounterId = cEntity.CounterId,
                                    Timestamp = value.Timestamp,
                                    Value = (double)value.Value
                                };
                                lstCounters.Add(nValue);
                            }
                        }

                    }

                    foreach (var batch in lstCounters.Batch(500))
                        await Dal.InsertCounterValueAsync(batch, true).ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
        }

        /// <inheritdoc />
        public async Task ProcessStatusMessageAsync(StatusItemCollection message)
        {
            if (message is null) return;
            using (Watch.Create("Processing StatusItemCollection Message", LogLevel.InfoBasic))
            {
                try
                {
                    await Dal.DeleteStatus(message.InstanceId).ConfigureAwait(false);

                    var item = NodeStatusItem.Create(message);

                    var status = new EntStatus
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
                    await Dal.InsertStatusAsync(status).ConfigureAwait(false);

                    if (item.Values != null)
                    {
                        var insertBuffer = new List<EntStatusValue>();

                        foreach (var value in item.Values)
                        {
                            insertBuffer.Add(new EntStatusValue
                            {
                                StatusId = status.StatusId,
                                Type = value.Type,
                                Key = value.Key,
                                Value = value.Value is IConvertible vConv ? vConv.ToString(CultureFormat) : value.Value?.ToString()
                            });
                        }

                        foreach(var batch in insertBuffer.Batch(100)) 
                            await Dal.InsertStatusValuesAsync(batch);
                    }
                }
                catch(Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
        }


        /// <inheritdoc />
        public async Task<Guid> EnsureCounter(ICounterItem counter)
        {
            var results = await Dal.GetCounter(counter.Environment, counter.Application, counter.Category, counter.Name);
            if (results.Count > 0)
            {
                return PostgresBindings.GetCounterEntity(results[0]).CounterId;
            }
            else
            {
                var ncounter = new EntCounter
                {
                    Environment = counter.Environment,
                    Application = counter.Application,
                    CounterId = Guid.NewGuid(),
                    Category = counter.Category,
                    Name = counter.Name,
                    Level = counter.Level,
                    Kind = counter.Kind,
                    Unit = counter.Unit,
                    Type = counter.Type
                };
                await Dal.InsertCounterAsync(new[] { ncounter }, true).ConfigureAwait(false);
                return ncounter.CounterId;
            }
        }

        /// <inheritdoc />
        public async Task InsertCounterValue(Guid counterId, CounterItemValue<double> value)
        {
            var nValue = new EntCounterValue
            {
                CounterId = counterId,
                Timestamp = value.Timestamp,
                Value = value.Value
            };
            await Dal.InsertCounterValueAsync(new[] { nValue }, true).ConfigureAwait(false);
        }
    }
}
