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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWCore.Collections;
using TWCore.Compression;
using TWCore.Diagnostics.Api.Models;
using TWCore.Diagnostics.Api.Models.Counters;
using TWCore.Diagnostics.Api.Models.Log;
using TWCore.Diagnostics.Api.Models.Status;
using TWCore.Diagnostics.Api.Models.Trace;
using TWCore.Diagnostics.Counters;
using TWCore.Diagnostics.Log;
using TWCore.Diagnostics.Status;
using TWCore.Serialization;
using TWCore.Serialization.NSerializer;

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres
{
    public class PostgresQueryHandler : IDiagnosticQueryHandler
    {
        private static readonly DiagnosticsSettings Settings = Core.GetSettings<DiagnosticsSettings>();
        private static readonly ICompressor Compressor = new GZipCompressor();
        private static readonly NBinarySerializer NBinarySerializer = new NBinarySerializer
        {
            Compressor = Compressor
        };
        private static readonly PostgresDal Dal = new PostgresDal();

        public void Init()
        {
        }

        public async Task<List<string>> GetEnvironmentsAsync()
        {
            var results = await Dal.GetAllEnvironments().ConfigureAwait(false);
            return results.Select(row => (string)row[0]).ToList();
        }

        public async Task<LogSummary> GetLogsApplicationsLevelsByEnvironmentAsync(string environment, DateTime fromDate, DateTime toDate)
        {
            var results = await Dal.GetLogLevelsByEnvironment(environment, fromDate, toDate).ConfigureAwait(false);

            var dctApplicationLevels = new Dictionary<string, ApplicationsLevels>();
            var dctLogLevels = new Dictionary<LogLevel, LogLevelTimes>();

            foreach (var value in results)
            {
                var valEnvironment = value.Get<string>("environment");
                var valApplication = value.Get<string>("application");
                var valDate = value.Get<DateTime>("date");
                var valLevel = value.Get<LogLevel>("level");
                var valCount = (int)value.Get<long>("count");

                if (!dctApplicationLevels.TryGetValue(valApplication, out var application))
                {
                    application = new ApplicationsLevels
                    {
                        Application = valApplication,
                        Levels = new List<LogLevelQuantity>()
                    };
                    dctApplicationLevels[valApplication] = application;
                }

                var appLevel = application.Levels.FirstOrDefault(al => al.Name == valLevel);
                if (appLevel == null)
                {
                    appLevel = new LogLevelQuantity
                    {
                        Name = valLevel,
                    };
                    application.Levels.Add(appLevel);
                }
                appLevel.Count += valCount;


                if (!dctLogLevels.TryGetValue(valLevel, out var levelTimes))
                {
                    levelTimes = new LogLevelTimes
                    {
                        Name = valLevel,
                        Series = new List<TimeCount>()
                    };
                    dctLogLevels[valLevel] = levelTimes;
                }
                levelTimes.Count += valCount;


                var levelSeries = levelTimes.Series.FirstOrDefault(ls => ls.Date == valDate);
                if (levelSeries == null)
                {
                    levelSeries = new TimeCount
                    {
                        Date = valDate
                    };
                    levelTimes.Series.Add(levelSeries);
                    levelTimes.Series.Sort((a, b) => a.Date.CompareTo(b.Date));
                }
                levelSeries.Count += valCount;

                application.Levels.Sort((a, b) => a.Name.CompareTo(b.Name));
            }

            var apps = dctApplicationLevels.Values.ToList();
            var levels = dctLogLevels.Values.ToList();
            apps.Sort((a, b) => a.Application.CompareTo(b.Application));
            levels.Sort((a, b) => a.Name.CompareTo(b.Name));
            return new LogSummary
            {
                Applications = apps,
                Levels = levels
            };
        }

        public async Task<PagedList<NodeLogItem>> GetLogsByApplicationLevelsEnvironmentAsync(string environment, string application, LogLevel? level, DateTime fromDate, DateTime toDate, int page, int pageSize = 50)
        {
            PostgresHelper.DbResult results;
            if (level.HasValue)
                results = await Dal.GetLogsByApplication(environment, application, level.Value, fromDate, toDate, page, pageSize).ConfigureAwait(false);
            else
                results = await Dal.GetLogsByApplication(environment, application, fromDate, toDate, page, pageSize).ConfigureAwait(false);

            var data = new List<NodeLogItem>();

            foreach (var row in results)
            {
                var item = GetLogItem(row);
                data.Add(item);
            }

            return new PagedList<NodeLogItem>
            {
                PageNumber = page,
                PageSize = pageSize,
                TotalResults = results.TotalCount,
                Data = data
            };
        }

        public async Task<PagedList<TraceResult>> GetTracesByEnvironmentAsync(string environment, DateTime fromDate, DateTime toDate, int page, int pageSize = 50)
        {
            var results = await Dal.GetTracesByEnvironment(environment, fromDate, toDate, page, pageSize).ConfigureAwait(false);

            var data = new List<TraceResult>();

            foreach (var row in results)
            {
                var item = new TraceResult
                {
                    Group = row.Get<string>("group"),
                    Count = (int)row.Get<long>("count"),
                    Start = row.Get<DateTime>("start"),
                    End = row.Get<DateTime>("end"),
                    HasErrors = row.Get<bool>("haserror")
                };
                data.Add(item);
            }

            return new PagedList<TraceResult>
            {
                PageNumber = page,
                PageSize = pageSize,
                TotalResults = results.TotalCount,
                Data = data
            };
        }

        public async Task<List<NodeTraceItem>> GetTracesByGroupIdAsync(string environment, string groupName)
        {
            var results = await Dal.GetTracesByGroupId(environment, groupName).ConfigureAwait(false);
            var data = new List<NodeTraceItem>();
            foreach (var row in results)
            {
                var item = GetTraceItem(row);
                data.Add(item);
            }
            return data;
        }


        public async Task<SerializedObject> GetTraceObjectAsync(string id)
        {
            var results = await Dal.GetTracesByTraceId(new Guid(id)).ConfigureAwait(false);
            NodeTraceItem traceItem = null;
            if (results.Count > 0)
                traceItem = GetTraceItem(results[0]);

            if (traceItem == null) return null;

            try
            {
                return await GetFromDisk(traceItem, ".nbin.gz").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Core.Log.Write(ex);
                throw;
            }

            #region Inner Methods
            async Task<SerializedObject> GetFromDisk(NodeTraceItem nodeTraceItem, string extension)
            {
                var bytes = await TraceDiskStorage.GetAsync(nodeTraceItem, extension).ConfigureAwait(false);
                if (bytes.IsGzip())
                    return NBinarySerializer.Deserialize<SerializedObject>(bytes);
                else
                    return bytes.DeserializeFromNBinary<SerializedObject>();
            }
            #endregion
        }

        /// <summary>
        /// Gets the Trace object
        /// </summary>
        /// <returns>The trace object</returns>
        /// <param name="id">Trace object id</param>
        public async Task<string> GetTraceAsync(string id, string traceName)
        {
            var results = await Dal.GetTracesByTraceId(new Guid(id)).ConfigureAwait(false);
            NodeTraceItem traceItem = null;
            if (results.Count > 0)
                traceItem = GetTraceItem(results[0]);

            if (traceItem == null) return null;

            var extension = traceName == "TraceXml" ? ".xml.gz" : traceName == "TraceJson" ? ".json.gz" : ".txt.gz";
            try
            {
                return await GetFromDisk(traceItem, extension).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Core.Log.Write(ex);
                throw;
            }

            #region Inner Methods
            async Task<string> GetFromDisk(NodeTraceItem nodeTraceItem, string ext)
            {
                var bytes = await TraceDiskStorage.GetAsync(nodeTraceItem, ext).ConfigureAwait(false);
                if (bytes.IsGzip())
                {
                    var desBytes = Compressor.Decompress(bytes);
                    return Encoding.UTF8.GetString(desBytes.AsSpan());
                }
                else
                {
                    return Encoding.UTF8.GetString(bytes.AsSpan());
                }
            }
            #endregion
        }

        /// <summary>
        /// Gets the Trace object in xml
        /// </summary>
        /// <returns>The trace object</returns>
        /// <param name="id">Trace object id</param>
        public Task<string> GetTraceXmlAsync(string id)
            => GetTraceAsync(id, "TraceXml");
        /// <summary>
        /// Gets the Trace object in json
        /// </summary>
        /// <returns>The trace object</returns>
        /// <param name="id">Trace object id</param>
        public Task<string> GetTraceJsonAsync(string id)
            => GetTraceAsync(id, "TraceJson");
        /// <summary>
		/// Gets the Trace object in txt
		/// </summary>
		/// <returns>The trace object</returns>
		/// <param name="id">Trace object id</param>
        public Task<string> GetTraceTxtAsync(string id)
            => GetTraceAsync(id, "TraceTxt");

        public async Task<SearchResults> SearchAsync(string environment, string searchTerm, DateTime fromDate, DateTime toDate)
        {
            var results = await Dal.Search(environment, searchTerm, fromDate, toDate, 10).ConfigureAwait(false);
            var groups = results.Select(row => (string)row[0]).ToList();
            var data = new List<NodeInfo>();

            foreach (var group in groups)
            {
                var logsResults = await Dal.GetLogsByGroup(environment, group, fromDate, toDate).ConfigureAwait(false);
                var tracesResults = await Dal.GetTracesByGroupId(environment, group).ConfigureAwait(false);

                foreach (var row in logsResults)
                    data.Add(GetLogItem(row));

                foreach (var row in tracesResults)
                    data.Add(GetTraceItem(row));
            }

            return new SearchResults
            {
                Data = data
            };
        }

        public async Task<KeyValue[]> GetMetadatas(string groupName)
        {
            var results = await Dal.GetMetadataByGroup(groupName).ConfigureAwait(false);
            var dict = new Dictionary<string, KeyValue>();
            foreach (var row in results)
            {
                var valKey = row.Get<string>("key");
                var valValue = row.Get<string>("value");
                if (!dict.ContainsKey(valKey))
                {
                    dict[valKey] = new KeyValue
                    {
                        Key = valKey,
                        Value = valValue
                    };
                }
            }
            return dict.Values.ToArray();
        }

        public async Task<PagedList<NodeStatusItem>> GetStatusesAsync(string environment, string machine, string application, DateTime fromDate, DateTime toDate, int page, int pageSize = 50)
        {
            var results = await Dal.GetStatuses(environment, machine, application, fromDate, toDate, page, pageSize).ConfigureAwait(false);
            var data = new List<NodeStatusItem>();
            foreach (var item in results)
            {
                var sStatusId = item.Get<Guid>("status_id");
                var sEnvironment = item.Get<string>("environment");
                var sMachine = item.Get<string>("machine");
                var sApplication = item.Get<string>("application");
                var sTimestamp = item.Get<DateTime>("timestamp");
                var sApplicationDisplay = item.Get<string>("application_display");
                var sElapsed = item.Get<decimal>("elapsed");
                var sStartTime = item.Get<DateTime>("start_time");
                var sDate = item.Get<DateTime>("date");
                var nodeStatus = new NodeStatusItem
                {
                    Application = sApplication,
                    ApplicationDisplayName = sApplicationDisplay,
                    Date = sDate,
                    ElapsedMilliseconds = (double)sElapsed,
                    Environment = sEnvironment,
                    Id = sStatusId.ToString(),
                    InstanceId = sStatusId,
                    Machine = sMachine,
                    StartTime = sStartTime,
                    Timestamp = sTimestamp,
                    Values = new List<NodeStatusItemValue>()
                };
                data.Add(nodeStatus);
            }
            var ids = data.Select(i => i.InstanceId).ToArray();
            var valuesResults = await Dal.GetStatusesValues(ids).ConfigureAwait(false);

            NodeStatusItem currentNode = null;
            foreach (var item in valuesResults)
            {
                var sStatusId = item.Get<Guid>("status_id");
                var sKey = item.Get<string>("key");
                var sValue = item.Get<string>("value");
                var sType = item.Get<StatusItemValueType>("type");

                if (currentNode == null || currentNode.InstanceId != sStatusId)
                    currentNode = data.First(i => i.InstanceId == sStatusId);

                currentNode.Values.Add(new NodeStatusItemValue
                {
                    Key = sKey,
                    Value = sValue,
                    Type = sType
                });
            }

            return new PagedList<NodeStatusItem>
            {
                Data = data,
                PageNumber = page,
                PageSize = pageSize,
                TotalResults = results.TotalCount
            };
        }

        public async Task<List<NodeStatusItem>> GetCurrentStatus(string environment, string machine, string application)
        {
            var results = await Dal.GetStatuses(environment, machine, application).ConfigureAwait(false);
            var data = new List<NodeStatusItem>();
            foreach(var item in results)
            {
                var sStatusId = item.Get<Guid>("status_id");
                var sEnvironment = item.Get<string>("environment");
                var sMachine = item.Get<string>("machine");
                var sApplication = item.Get<string>("application");
                var sTimestamp = item.Get<DateTime>("timestamp");
                var sApplicationDisplay = item.Get<string>("application_display");
                var sElapsed = item.Get<decimal>("elapsed");
                var sStartTime = item.Get<DateTime>("start_time");
                var sDate = item.Get<DateTime>("date");
                var nodeStatus = new NodeStatusItem
                {
                    Application = sApplication,
                    ApplicationDisplayName = sApplicationDisplay,
                    Date = sDate,
                    ElapsedMilliseconds = (double)sElapsed,
                    Environment = sEnvironment,
                    Id = sStatusId.ToString(),
                    InstanceId = sStatusId,
                    Machine = sMachine,
                    StartTime = sStartTime,
                    Timestamp = sTimestamp,
                    Values = new List<NodeStatusItemValue>()
                };
                data.Add(nodeStatus);
            }

            var rData = data.GroupBy(i => new { i.Environment, i.Machine, i.Application })
                .Select(i => i.First())
                .ToList();

            var ids = rData.Select(i => i.InstanceId).ToArray();
            var valuesResults = await Dal.GetStatusesValues(ids).ConfigureAwait(false);

            NodeStatusItem currentNode = null;
            foreach(var item in valuesResults)
            {
                var sStatusId = item.Get<Guid>("status_id");
                var sKey = item.Get<string>("key");
                var sValue = item.Get<string>("value");
                var sType = item.Get<StatusItemValueType>("type");

                if (currentNode == null || currentNode.InstanceId != sStatusId)
                    currentNode = rData.First(i => i.InstanceId == sStatusId);

                currentNode.Values.Add(new NodeStatusItemValue
                {
                    Key = sKey,
                    Value = sValue,
                    Type = sType
                });
            }

            return rData;
        }

        public async Task<List<NodeCountersQueryItem>> GetCounters(string environment)
        {
            var results = await Dal.GetCounters(environment).ConfigureAwait(false);
            var data = new List<NodeCountersQueryItem>();
            foreach (var item in results)
                data.Add(GetCounterItem(item));
            return data;
        }

        public async Task<NodeCountersQueryItem> GetCounter(Guid counterId)
        {
            var results = await Dal.GetCounter(counterId).ConfigureAwait(false);
            if (results.Count == 0) return null;
            return GetCounterItem(results[0]);
        }

        public async Task<List<NodeCountersQueryValue>> GetCounterValues(Guid counterId, DateTime fromDate, DateTime toDate, int limit = 3600)
        {
            var results = await Dal.GetCountersValues(counterId, fromDate, toDate).ConfigureAwait(false);
            var counterValues = new List<NodeCountersQueryValue>();
            foreach (var row in results)
            {
                var item = new NodeCountersQueryValue
                {
                    Id = row.Get<Guid>("counter_id").ToString(),
                    Timestamp = row.Get<DateTime>("timestamp"),
                    Value = row.Get<float>("value")
                };
                counterValues.Add(item);
            }
            return counterValues;
        }

        public async Task<List<NodeLastCountersValue>> GetLastCounterValues(Guid counterId, CounterValuesDivision valuesDivision, int samples = 250, DateTime? lastDate = null)
        {
            var counterDataTask = GetCounter(counterId);
            var toDate = Core.Now;
            var fromDate = toDate;

            #region Values Division
            switch (valuesDivision)
            {
                case CounterValuesDivision.QuarterDay:
                    fromDate = toDate.AddHours(-6);
                    if (samples == 0) samples = 36;
                    break;
                case CounterValuesDivision.HalfDay:
                    fromDate = toDate.AddHours(-12);
                    if (samples == 0) samples = 48;
                    break;
                case CounterValuesDivision.Day:
                    fromDate = toDate.AddDays(-1);
                    if (samples == 0) samples = 48;
                    break;
                case CounterValuesDivision.Week:
                    fromDate = toDate.AddDays(-7);
                    if (samples == 0) samples = 84;
                    break;
                case CounterValuesDivision.Month:
                    fromDate = toDate.AddMonths(-1);
                    if (samples == 0) samples = 60;
                    break;
                case CounterValuesDivision.TwoMonths:
                    fromDate = toDate.AddMonths(-2);
                    if (samples == 0) samples = 60;
                    break;
                case CounterValuesDivision.QuarterYear:
                    fromDate = toDate.AddMonths(-3);
                    if (samples == 0) samples = 90;
                    break;
                case CounterValuesDivision.HalfYear:
                    fromDate = toDate.AddMonths(-6);
                    if (samples == 0) samples = 90;
                    break;
                case CounterValuesDivision.Year:
                    fromDate = toDate.AddYears(-1);
                    if (samples == 0) samples = 73;
                    break;
            }
            #endregion

            var timeInterval = toDate.Subtract(fromDate);
            var minutes = (timeInterval.TotalMinutes / samples);
            var lstValues = new List<NodeLastCountersValue>();

            #region Get Values
            var results = await Dal.GetCountersValues(counterId, fromDate, toDate).ConfigureAwait(false);
            var counterValues = new List<NodeCountersQueryValue>();
            foreach (var row in results)
            {
                var item = new NodeCountersQueryValue
                {
                    Id = row.Get<Guid>("counter_id").ToString(),
                    Timestamp = row.Get<DateTime>("timestamp"),
                    Value = row.Get<float>("value")
                };
                counterValues.Add(item);
            }
            #endregion

            #region List Values
            for (var i = 0; i < samples; i++)
            {
                if (i == 0)
                    lstValues.Add(new NodeLastCountersValue { Timestamp = fromDate });
                else
                    lstValues.Add(new NodeLastCountersValue { Timestamp = lstValues[i - 1].Timestamp.AddMinutes(minutes) });
            }
            #endregion

            var counterData = await counterDataTask.ConfigureAwait(false);

            #region Fill Values
            for (var i = 0; i < lstValues.Count; i++)
            {
                IEnumerable<NodeCountersQueryValue> cValues;
                var currentItem = lstValues[i];
                if (i == lstValues.Count - 1)
                {
                    cValues = counterValues.Where(item => item.Timestamp >= currentItem.Timestamp);
                }
                else
                {
                    var tDate = lstValues[i + 1].Timestamp;
                    cValues = counterValues.Where(item => item.Timestamp >= currentItem.Timestamp && item.Timestamp < tDate);
                }
                double res = 0;
                switch (counterData.Type)
                {
                    case Counters.CounterType.Average:
                        res = cValues.Any() ? cValues.Average(item => (double)Convert.ChangeType(item.Value, TypeCode.Double)) : 0;
                        break;
                    case Counters.CounterType.Cumulative:
                        res = cValues.Sum(item => (double)Convert.ChangeType(item.Value, TypeCode.Double));
                        //if (i > 0)
                        //    res += (double)lstValues[i - 1].Value;
                        break;
                    case Counters.CounterType.Current:
                        res = cValues.Sum(item => (double)Convert.ChangeType(item.Value, TypeCode.Double));
                        break;
                }
                currentItem.Timestamp = currentItem.Timestamp.TruncateTo(TimeSpan.FromMinutes(1));
                currentItem.Value = res;
            }
            #endregion

            #region Find LastDate
            if (lastDate.HasValue)
            {
                var dateIndex = lstValues.FindLastIndex(item => item.Timestamp == lastDate.Value);
                if (dateIndex > -1)
                    return lstValues.Skip(dateIndex).ToList();
            }
            #endregion

            return lstValues;
        }



        private static NodeLogItem GetLogItem(PostgresHelper.DbRow row)
        {
            var item = new NodeLogItem
            {
                Application = row.Get<string>("application"),
                Assembly = row.Get<string>("assembly"),
                Code = row.Get<string>("code"),
                Environment = row.Get<string>("environment"),
                Group = row.Get<string>("group"),
                Id = row.Get<Guid>("log_id").ToString(),
                Level = row.Get<LogLevel>("level"),
                LogId = row.Get<Guid>("log_id"),
                Machine = row.Get<string>("machine"),
                Message = row.Get<string>("message"),
                Timestamp = row.Get<DateTime>("timestamp"),
                Type = row.Get<string>("type"),
            };
            var ex = row.Get<string>("exception");
            if (ex != null)
                item.Exception = ex.DeserializeFromJson<SerializableException>();
            return item;
        }

        private static NodeTraceItem GetTraceItem(PostgresHelper.DbRow row)
        {
            return new NodeTraceItem
            {
                Application = row.Get<string>("application"),
                Environment = row.Get<string>("environment"),
                Formats = row.Get<string[]>("formats"),
                Group = row.Get<string>("group"),
                Id = row.Get<Guid>("trace_id").ToString(),
                Machine = row.Get<string>("machine"),
                Name = row.Get<string>("name"),
                Tags = row.Get<string>("tags"),
                Timestamp = row.Get<DateTime>("timestamp"),
                TraceId = row.Get<Guid>("trace_id")
            };
        }

        private static NodeCountersQueryItem GetCounterItem(PostgresHelper.DbRow row)
        {
            return new NodeCountersQueryItem
            {
                Application = row.Get<string>("application"),
                Category = row.Get<string>("category"),
                CountersId = row.Get<Guid>("counter_id"),
                Name = row.Get<string>("name"),
                TypeOfValue = row.Get<string>("typeofvalue"),
                Kind = row.Get<CounterKind>("kind"),
                Level = row.Get<CounterLevel>("level"),
                Type = row.Get<CounterType>("type"),
                Unit = row.Get<CounterUnit>("unit"),
            };
        }

    }
}
