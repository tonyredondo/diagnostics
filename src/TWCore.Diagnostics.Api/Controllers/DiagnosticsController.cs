using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TWCore.Collections;
using TWCore.Diagnostics.Api.MessageHandlers;
using TWCore.Diagnostics.Counters;
using TWCore.Diagnostics.Log;
using TWCore.Diagnostics.Trace.Storages;
using TWCore.Security;

namespace TWCore.Diagnostics.Api.Controllers
{
    [Route("api/diagnostics")]
    public class DiagnosticsController : Controller
    {
        [HttpPost("log")]
        public bool PostLogItem([FromBody]ExternalLogItem item)
        {
            if (item == null) return false;
            if (string.IsNullOrWhiteSpace(item.EnvironmentName)) return false;
            if (string.IsNullOrWhiteSpace(item.GroupName)) return false;
            if (string.IsNullOrWhiteSpace(item.Message)) return false;
            var logItem = new LogItem
            {
                MachineName = item.MachineName,
                ApplicationName = item.ApplicationName,
                ProcessName = item.ProcessName,
                AssemblyName = item.AssemblyName,
                TypeName = item.TypeName,
                Level = item.Level,
                Message = item.Message,
                Timestamp = item.Timestamp ?? Core.Now,
                GroupName = item.GroupName,
                Exception = item.Exception,
                Code = item.Code,
                EnvironmentName = item.EnvironmentName,
                Id = Guid.NewGuid(),
                InstanceId = item.ApplicationName?.GetHashSHA1Guid() ?? Guid.Empty
            };
            _ = Process(logItem);
            return true;

            async Task Process(LogItem lItem)
            {
                try
                {
                    await DbHandlers.Instance.Messages.ProcessLogItemsMessageAsync(new List<LogItem> { lItem }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
        }

        [HttpPost("metadata")]
        public bool PostGroupMetadata([FromBody]ExternalGroupMetadata item)
        {
            if (item == null) return false;
            if (string.IsNullOrWhiteSpace(item.EnvironmentName)) return false;
            if (string.IsNullOrWhiteSpace(item.GroupName)) return false;
            if (item.Items?.Any() != true) return false;
            var groupMetadata = new GroupMetadata
            {
                InstanceId = item.ApplicationName?.GetHashSHA1Guid() ?? Guid.Empty,
                GroupName = item.GroupName,
                Timestamp = item.Timestamp ?? Core.Now,
                Items = item.Items
            };
            _ = Process(groupMetadata);
            return true;

            async Task Process(GroupMetadata gMeta)
            {
                try
                {
                    await DbHandlers.Instance.Messages.ProcessGroupMetadataMessageAsync(new List<GroupMetadata> { gMeta }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
        }

        [HttpPost("trace")]
        public bool PostTraceItem([FromBody]ExternalTraceItem item)
        {
            if (item == null) return false;
            if (string.IsNullOrWhiteSpace(item.EnvironmentName)) return false;
            if (string.IsNullOrWhiteSpace(item.GroupName)) return false;
            var traceItem = new MessagingTraceItem
            {
                MachineName = item.MachineName,
                ApplicationName = item.ApplicationName,
                Tags = item.Tags,
                GroupName = item.GroupName,
                TraceName = item.TraceName,
                TraceObject = item.TraceData,
                Timestamp = item.Timestamp ?? Core.Now,
                EnvironmentName = item.EnvironmentName,
                Id = Guid.NewGuid(),
                InstanceId = item.ApplicationName?.GetHashSHA1Guid() ?? Guid.Empty
            };
            _ = Process(traceItem);
            return true;


            async Task Process(MessagingTraceItem mTItem)
            {
                try
                {
                    await DbHandlers.Instance.Messages.ProcessTraceItemsMessageAsync(new List<MessagingTraceItem> { mTItem }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
        }

        [HttpPost("counter")]
        public async Task<Guid?> PostCounter([FromBody] ExternalCounter item)
        {
            if (item == null) return null;
            if (string.IsNullOrWhiteSpace(item.Environment)) return null;

            try
            {
                return await DbHandlers.Instance.Messages.EnsureCounter(item).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Core.Log.Write(ex);
                return null;
            }
        }

        [HttpPost("counter/value")]
        public bool PostCounterValue([FromBody] ExternalCounterValue item)
        {
            if (item == null) return false;
            if (item.CounterId == Guid.Empty) return false;
            var cItemValue = new CounterItemValue<double>
            {
                Timestamp = item.Timestamp ?? Core.Now,
                Value = item.Value
            };
            _ = Process(item.CounterId, cItemValue);
            return true;

            async Task Process(Guid id, CounterItemValue<double> mItem)
            {
                try
                {
                    await DbHandlers.Instance.Messages.InsertCounterValue(id, mItem).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
        }

        [HttpPost("ingest")]
        public bool PostIngest([FromBody] IngestPayload[] payload)
        {
            if (payload == null || payload.Length == 0) return false;

            List<LogItem> lstLogItems = null;
            List<GroupMetadata> lstGroupMetadata = null;
            List<MessagingTraceItem> lstTraceItem = null;

            foreach (var item in payload)
            {
                if (string.IsNullOrWhiteSpace(item.EnvironmentName)) continue;
                if (string.IsNullOrWhiteSpace(item.GroupName)) continue;

                var instanceId = item.ApplicationName?.GetHashSHA1Guid() ?? Guid.Empty;
                var timestamp = item.Timestamp ?? Core.Now;

                if (!string.IsNullOrEmpty(item.Message))
                {
                    if (lstLogItems == null)
                        lstLogItems = new List<LogItem>();

                    lstLogItems.Add(new LogItem
                    {
                        MachineName = item.MachineName,
                        ApplicationName = item.ApplicationName,
                        ProcessName = item.ProcessName,
                        AssemblyName = item.AssemblyName,
                        TypeName = item.TypeName,
                        Level = item.Level,
                        Message = item.Message,
                        Timestamp = timestamp,
                        GroupName = item.GroupName,
                        Exception = item.Exception,
                        Code = item.Code,
                        EnvironmentName = item.EnvironmentName,
                        Id = Guid.NewGuid(),
                        InstanceId = instanceId
                    });
                }
                if (item.Metadata != null && item.Metadata.Length > 0)
                {
                    if (lstGroupMetadata == null)
                        lstGroupMetadata = new List<GroupMetadata>();

                    lstGroupMetadata.Add(new GroupMetadata
                    {
                        InstanceId = instanceId,
                        GroupName = item.GroupName,
                        Timestamp = timestamp,
                        Items = item.Metadata
                    });
                }
                if (!string.IsNullOrEmpty(item.TraceName) || !string.IsNullOrEmpty(item.TraceData))
                {
                    if (lstTraceItem == null)
                        lstTraceItem = new List<MessagingTraceItem>();

                    lstTraceItem.Add(new MessagingTraceItem
                    {
                        MachineName = item.MachineName,
                        ApplicationName = item.ApplicationName,
                        Tags = item.Tags.Select(tag => $"{tag.Key}: {tag.Value}").ToArray(),
                        GroupName = item.GroupName,
                        TraceName = item.TraceName,
                        TraceObject = item.TraceData,
                        Timestamp = timestamp,
                        EnvironmentName = item.EnvironmentName,
                        Id = Guid.NewGuid(),
                        InstanceId = instanceId
                    });
                }

            }

            _ = ProcessLog(lstLogItems);
            _ = ProcessMetadata(lstGroupMetadata);
            _ = ProcessTrace(lstTraceItem);

            return lstLogItems != null || lstGroupMetadata != null || lstTraceItem != null;

            async Task ProcessLog(List<LogItem> logItems)
            {
                if (logItems == null) return;
                try
                {
                    await DbHandlers.Instance.Messages.ProcessLogItemsMessageAsync(logItems).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
            async Task ProcessMetadata(List<GroupMetadata> metaItems)
            {
                if (metaItems == null) return;
                try
                {
                    await DbHandlers.Instance.Messages.ProcessGroupMetadataMessageAsync(metaItems).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
            async Task ProcessTrace(List<MessagingTraceItem> traceItems)
            {
                if (traceItems == null) return;
                try
                {
                    await DbHandlers.Instance.Messages.ProcessTraceItemsMessageAsync(traceItems).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                }
            }
        }
    }

    /// <summary>
    /// External Log item
    /// </summary>
    [DataContract]
    public class ExternalLogItem
    {
        #region Properties
        /// <summary>
        /// Environment name
        /// </summary>
        [XmlAttribute, DataMember]
        public string EnvironmentName { get; set; }
        /// <summary>
        /// Machine name
        /// </summary>
        [XmlAttribute, DataMember]
        public string MachineName { get; set; }
        /// <summary>
        /// Application name
        /// </summary>
        [XmlAttribute, DataMember]
        public string ApplicationName { get; set; }
        /// <summary>
        /// Process name
        /// </summary>
        [XmlAttribute, DataMember]
        public string ProcessName { get; set; }
        /// <summary>
        /// Assembly name
        /// </summary>
        [XmlAttribute, DataMember]
        public string AssemblyName { get; set; }
        /// <summary>
        /// Type name
        /// </summary>
        [XmlAttribute, DataMember]
        public string TypeName { get; set; }
        /// <summary>
        /// Log level
        /// </summary>
        [XmlAttribute, DataMember]
        public LogLevel Level { get; set; }
        /// <summary>
        /// Code
        /// </summary>
        [XmlAttribute, DataMember]
        public string Code { get; set; }
        /// <summary>
        /// Message
        /// </summary>
        [XmlAttribute, DataMember]
        public string Message { get; set; }
        /// <summary>
        /// Item timestamp
        /// </summary>
        [XmlElement, DataMember]
        public DateTime? Timestamp { get; set; }
        /// <summary>
        /// Message group name
        /// </summary>
        [XmlAttribute, DataMember]
        public string GroupName { get; set; }
        /// <summary>
        /// If is an error log item, the exception object instance
        /// </summary>
        [XmlElement, DataMember]
        public SerializableException Exception { get; set; }
        #endregion
    }
    /// <summary>
    /// External Group metadata
    /// </summary>
    [XmlRoot("GroupMetadata")]
    public class ExternalGroupMetadata
    {
        /// <summary>
        /// Environment name
        /// </summary>
        [XmlAttribute, DataMember]
        public string EnvironmentName { get; set; }
        /// <summary>
        /// Application name
        /// </summary>
        [XmlAttribute, DataMember]
        public string ApplicationName { get; set; }
        /// <inheritdoc />
        /// <summary>
        /// Item timestamp
        /// </summary>
        [XmlElement, DataMember]
        public DateTime? Timestamp { get; set; }
        /// <summary>
        /// Gets the name of the group.
        /// </summary>
        /// <value>The name of the group.</value>
        [XmlAttribute, DataMember]
        public string GroupName { get; set; }
        /// <summary>
        /// Gets the Metadata Items
        /// </summary>
        /// <value>The metadata items</value>
        [XmlArray("Items"), XmlArrayItem("Item"), DataMember]
        public KeyValue[] Items { get; set; }
    }
    /// <summary>
    /// External Trace Item
    /// </summary>
    [DataContract]
    public class ExternalTraceItem
    {
        /// <summary>
        /// Environment name
        /// </summary>
        [XmlAttribute, DataMember]
        public string EnvironmentName { get; set; }
        /// <summary>
        /// Machine name
        /// </summary>
        [XmlAttribute, DataMember]
        public string MachineName { get; set; }
        /// <summary>
        /// Application name
        /// </summary>
        [XmlAttribute, DataMember]
        public string ApplicationName { get; set; }
        /// <summary>
        /// Tags
        /// </summary>
        [XmlElement, DataMember]
        public string[] Tags { get; set; }
        /// <summary>
        /// Trace group name
        /// </summary>
        [XmlAttribute, DataMember]
        public string GroupName { get; set; }
        /// <summary>
        /// Trace Name
        /// </summary>
        [XmlAttribute, DataMember]
        public string TraceName { get; set; }
        /// <summary>
        /// Trace Data
        /// </summary>
        [XmlElement, DataMember]
        public string TraceData { get; set; }
        /// <summary>
        /// Item timestamp
        /// </summary>
        [XmlElement, DataMember]
        public DateTime? Timestamp { get; set; }

    }
    /// <summary>
    /// External counter
    /// </summary>
    [DataContract]
    public class ExternalCounter : ICounterItem
    {
        /// <summary>
        /// Environment
        /// </summary>
        [XmlAttribute, DataMember]
        public string Environment { get; set; }
        /// <summary>
        /// Application name
        /// </summary>
        [XmlAttribute, DataMember]
        public string Application { get; set; }
        /// <summary>
        /// Counter category
        /// </summary>
        [XmlAttribute, DataMember]
        public string Category { get; set; }
        /// <summary>
        /// Counter name
        /// </summary>
        [XmlAttribute, DataMember]
        public string Name { get; set; }
        /// <summary>
        /// Counter type
        /// </summary>
        [XmlAttribute, DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public CounterType Type { get; set; }
        /// <summary>
        /// Counter level
        /// </summary>
        [XmlAttribute, DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public CounterLevel Level { get; set; }
        /// <summary>
        /// Counter kind
        /// </summary>
        [XmlAttribute, DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public CounterKind Kind { get; set; }
        /// <summary>
        /// Counter unit
        /// </summary>
        [XmlAttribute, DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public CounterUnit Unit { get; set; }


        int ICounterItem.Count => 0;
        Type ICounterItem.TypeOfValue => null;
    }

    [DataContract]
    public class ExternalCounterValue
    {
        /// <summary>
        /// Counter id
        /// </summary>
        [XmlAttribute, DataMember]
        public Guid CounterId { get; set; }
        /// <summary>
        /// Counter timestamp
        /// </summary>
        [DataMember]
        public DateTime? Timestamp { get; set; }
        /// <summary>
        /// Counter value
        /// </summary>
        [XmlAttribute, DataMember]
        public double Value { get; set; }
    }


    //*****************************************************************************
    public class IngestPayload
    {
        /// <summary>
        /// Environment name
        /// </summary>
        [XmlAttribute, DataMember]
        public string EnvironmentName { get; set; }
        /// <summary>
        /// Machine name
        /// </summary>
        [XmlAttribute, DataMember]
        public string MachineName { get; set; }
        /// <summary>
        /// Application name
        /// </summary>
        [XmlAttribute, DataMember]
        public string ApplicationName { get; set; }
        /// <summary>
        /// Process name
        /// </summary>
        [XmlAttribute, DataMember]
        public string ProcessName { get; set; }
        /// <summary>
        /// Assembly name
        /// </summary>
        [XmlAttribute, DataMember]
        public string AssemblyName { get; set; }
        /// <summary>
        /// Type name
        /// </summary>
        [XmlAttribute, DataMember]
        public string TypeName { get; set; }
        /// <summary>
        /// Log level
        /// </summary>
        [XmlAttribute, DataMember]
        public LogLevel Level { get; set; }
        /// <summary>
        /// Code
        /// </summary>
        [XmlAttribute, DataMember]
        public string Code { get; set; }
        /// <summary>
        /// Message
        /// </summary>
        [XmlAttribute, DataMember]
        public string Message { get; set; }
        /// <summary>
        /// Item timestamp
        /// </summary>
        [XmlElement, DataMember]
        public DateTime? Timestamp { get; set; }
        /// <summary>
        /// Message group name
        /// </summary>
        [XmlAttribute, DataMember]
        public string GroupName { get; set; }
        /// <summary>
        /// If is an error log item, the exception object instance
        /// </summary>
        [XmlElement, DataMember]
        public SerializableException Exception { get; set; }

        /// <summary>
        /// Gets the Metadata Items
        /// </summary>
        /// <value>The metadata items</value>
        [XmlArray("Items"), XmlArrayItem("Item"), DataMember]
        public KeyValue[] Metadata { get; set; }

        /// <summary>
        /// Trace Name
        /// </summary>
        [XmlAttribute, DataMember]
        public string TraceName { get; set; }
        /// <summary>
        /// Trace Data
        /// </summary>
        [XmlElement, DataMember]
        public string TraceData { get; set; }
        /// <summary>
        /// Gets the Tags Items
        /// </summary>
        /// <value>The metadata items</value>
        [XmlArray("Items"), XmlArrayItem("Item"), DataMember]
        public KeyValue[] Tags { get; set; }
    }
}