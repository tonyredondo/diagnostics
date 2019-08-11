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
using TWCore.Diagnostics.Api.Models.Groups;
using TWCore.Diagnostics.Api.Models.Log;
using TWCore.Diagnostics.Api.Models.Status;
using TWCore.Diagnostics.Api.Models.Trace;
using TWCore.Diagnostics.Log;
using TWCore.Diagnostics.Status;
using TWCore.Serialization;
using TWCore.Serialization.NSerializer;

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres
{
    public class PostgresQueryHandler : IDiagnosticQueryHandler
    {
        #region Fields
        private static readonly DiagnosticsSettings Settings = Core.GetSettings<DiagnosticsSettings>();
        private static readonly ICompressor Compressor = new GZipCompressor();
        private static readonly NBinarySerializer NBinarySerializer = new NBinarySerializer
        {
            Compressor = Compressor
        };
        private static readonly PostgresDal Dal = new PostgresDal();
        #endregion

        /// <inheritdoc />
        public void Init()
        {
        }

        /// <inheritdoc />
        public async Task<List<string>> GetEnvironmentsAsync()
        {
            var results = await Dal.GetAllEnvironments().ConfigureAwait(false);
            return results.Select(row => (string)row[0]).ToList();
        }

        /// <inheritdoc />
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

                if (valApplication == null) continue;
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

        /// <inheritdoc />
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
                var item = PostgresBindings.GetLogItem(row);
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

        /// <inheritdoc />
        public async Task<PagedList<TraceResult>> GetTracesByEnvironmentAsync(string environment, DateTime fromDate, DateTime toDate, bool withErrorsOnly, int page, int pageSize = 50)
        {
            var results = withErrorsOnly ?
                await Dal.GetTracesByEnvironmentWithErrors(environment, fromDate, toDate, page, pageSize).ConfigureAwait(false) :
                await Dal.GetTracesByEnvironment(environment, fromDate, toDate, page, pageSize).ConfigureAwait(false);

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

        /// <inheritdoc />
        public async Task<List<NodeTraceItem>> GetTracesByGroupIdAsync(string environment, string groupName)
        {
            var results = await Dal.GetTracesByGroupId(environment, groupName).ConfigureAwait(false);
            var data = new List<NodeTraceItem>();
            foreach (var row in results)
            {
                var item = PostgresBindings.GetTraceItem(row);
                data.Add(item);
            }
            return data;
        }

        /// <inheritdoc />
        public async Task<SerializedObject> GetTraceObjectAsync(string id)
        {
            var results = await Dal.GetTracesByTraceId(new Guid(id)).ConfigureAwait(false);
            NodeTraceItem traceItem = null;
            if (results.Count > 0)
                traceItem = PostgresBindings.GetTraceItem(results[0]);

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

        /// <inheritdoc />
        public async Task<string> GetTraceAsync(string id, string traceName)
        {
            var results = await Dal.GetTracesByTraceId(new Guid(id)).ConfigureAwait(false);
            NodeTraceItem traceItem = null;
            if (results.Count > 0)
                traceItem = PostgresBindings.GetTraceItem(results[0]);

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

        /// <inheritdoc />
        public Task<string> GetTraceXmlAsync(string id)
            => GetTraceAsync(id, "TraceXml");

        /// <inheritdoc />
        public Task<string> GetTraceJsonAsync(string id)
            => GetTraceAsync(id, "TraceJson");

        /// <inheritdoc />
        public Task<string> GetTraceTxtAsync(string id)
            => GetTraceAsync(id, "TraceTxt");

        /// <inheritdoc />
        public async Task<PagedList<GroupResult>> GetGroupsByEnvironmentAsync(string environment, DateTime fromDate, DateTime toDate, bool withErrorsOnly, int page, int pageSize = 50)
        {
            var results = withErrorsOnly ?
                await Dal.GetGroupsByEnvironmentWithErrors(environment, fromDate, toDate, page, pageSize).ConfigureAwait(false) :
                await Dal.GetGroupsByEnvironment(environment, fromDate, toDate, page, pageSize).ConfigureAwait(false);

            var data = new List<GroupResult>();

            foreach (var row in results)
            {
                var item = new GroupResult
                {
                    Group = row.Get<string>("group"),
                    LogsCount = (int)row.Get<long>("logscount"),
                    TracesCount = (int)row.Get<long>("tracescount"),
                    Start = row.Get<DateTime>("start"),
                    End = row.Get<DateTime>("end"),
                    HasErrors = row.Get<bool>("haserror")
                };
                data.Add(item);
            }

            return new PagedList<GroupResult>
            {
                PageNumber = page,
                PageSize = pageSize,
                TotalResults = results.TotalCount,
                Data = data
            };
        }

        /// <inheritdoc />
        public async Task<GroupData> GetGroupDataAsync(string environment, string group)
        {
            var results = new GroupData
            {
                Environment = environment,
                Group = group,
                Metadata = null,
                Data = new List<NodeInfo>()
            };

            var metadataTask = Dal.GetMetadataByGroup(environment, group);
            var tracesTask = Dal.GetTracesByGroupId(environment, group);
            var logsTask = Dal.GetLogsByGroup(environment, group);

            var metadataResults = await metadataTask.ConfigureAwait(false);
            var dict = new Dictionary<string, KeyValue>();
            foreach (var row in metadataResults)
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
            results.Metadata = dict.Values.OrderBy(i => i.Key).ToArray();

            var tracesResults = await tracesTask.ConfigureAwait(false);
            foreach (var row in tracesResults)
                results.Data.Add(PostgresBindings.GetTraceItem(row));

            var logsResults = await logsTask.ConfigureAwait(false);
            foreach (var row in logsResults)
                results.Data.Add(PostgresBindings.GetLogItem(row));

            results.Data.Sort((a, b) =>
            {
                return a.Timestamp.CompareTo(b.Timestamp);
            });

            return results;
        }

        /// <inheritdoc />
        public async Task<List<string>> GroupSearchAsync(string environment, string searchTerm, DateTime fromDate, DateTime toDate)
        {
            PostgresHelper.DbResult results;
            if (searchTerm == null)
                return new List<string>();

            var useGeneralSearch = true;
            var useExact = true;
            string key = null;
            string value = null;
            if (searchTerm.StartsWith("LIKE:", StringComparison.OrdinalIgnoreCase))
            {
                searchTerm = searchTerm.Substring(5).Trim();
                useExact = false;
            }
            else if (searchTerm.StartsWith("~", StringComparison.OrdinalIgnoreCase))
            {
                searchTerm = searchTerm.Substring(1).Trim();
                useExact = false;
            }
            else if (searchTerm.Contains("="))
            {
                useGeneralSearch = false;
                var values = searchTerm.Split('=');
                key = values[0].Trim();
                value = values.Length > 2 ? string.Join('=', values.Skip(1)) : values.Length > 1 ? values[1].Trim() : null;
                useExact = true;
            }
            else if (searchTerm.Contains("~"))
            {
                useGeneralSearch = false;
                var values = searchTerm.Split('~');
                key = values[0].Trim();
                value = values.Length > 2 ? string.Join('=', values.Skip(1)) : values.Length > 1 ? values[1].Trim() : null;
                useExact = false;
            }

            if (useGeneralSearch)
            {
                if (Guid.TryParse(searchTerm, out _))
                    useExact = true;

                if (useExact)
                    results = await Dal.SearchExact(environment, searchTerm, fromDate, toDate, 100).ConfigureAwait(false);
                else
                    results = await Dal.Search(environment, searchTerm, fromDate, toDate, 100).ConfigureAwait(false);
            }
            else if (value != null)
            {
                if (Guid.TryParse(value, out _))
                    useExact = true;

                if (useExact)
                    results = await Dal.SearchByMetadataExact(environment, key, value, fromDate, toDate, 100).ConfigureAwait(false);
                else
                    results = await Dal.SearchByMetadata(environment, key, value, fromDate, toDate, 100).ConfigureAwait(false);
            }
            else
            {
                return new List<string>();
            }

            return results.Select(row => (string)row[0]).ToList();
        }

        /// <inheritdoc />
        public async Task<SearchResults> SearchAsync(string environment, string searchTerm, DateTime fromDate, DateTime toDate)
        {
            PostgresHelper.DbResult results;
            if (searchTerm == null)
                return new SearchResults();

            var useGeneralSearch = true;
            var useExact = true;
            string key = null;
            string value = null;
            if (searchTerm.StartsWith("LIKE:", StringComparison.OrdinalIgnoreCase))
            {
                searchTerm = searchTerm.Substring(5).Trim();
                useExact = false;
            }
            else if (searchTerm.StartsWith("~", StringComparison.OrdinalIgnoreCase))
            {
                searchTerm = searchTerm.Substring(1).Trim();
                useExact = false;
            }
            else if (searchTerm.Contains("="))
            {
                useGeneralSearch = false;
                var values = searchTerm.Split('=');
                key = values[0].Trim();
                value = values.Length > 2 ? string.Join('=', values.Skip(1)) : values.Length > 1 ? values[1].Trim() : null;
                useExact = true;
            }
            else if (searchTerm.Contains("~"))
            {
                useGeneralSearch = false;
                var values = searchTerm.Split('~');
                key = values[0].Trim();
                value = values.Length > 2 ? string.Join('=', values.Skip(1)) : values.Length > 1 ? values[1].Trim() : null;
                useExact = false;
            }

            if (useGeneralSearch)
            {
                if (Guid.TryParse(searchTerm, out _))
                    useExact = true;

                if (useExact)
                    results = await Dal.SearchExact(environment, searchTerm, fromDate, toDate, 15).ConfigureAwait(false);
                else
                    results = await Dal.Search(environment, searchTerm, fromDate, toDate, 15).ConfigureAwait(false);
            }
            else if (value != null)
            {
                if (Guid.TryParse(value, out _))
                    useExact = true;

                if (useExact)
                    results = await Dal.SearchByMetadataExact(environment, key, value, fromDate, toDate, 15).ConfigureAwait(false);
                else
                    results = await Dal.SearchByMetadata(environment, key, value, fromDate, toDate, 15).ConfigureAwait(false);
            }
            else
            {
                results = new PostgresHelper.DbResult();
            }

            var groups = results.Select(row => (string)row[0]).ToList();
            var data = new List<NodeInfo>();

            foreach (var group in groups)
            {
                var logsResults = await Dal.GetLogsByGroup(environment, group).ConfigureAwait(false);
                var tracesResults = await Dal.GetTracesByGroupId(environment, group).ConfigureAwait(false);

                foreach (var row in logsResults)
                    data.Add(PostgresBindings.GetLogItem(row));

                foreach (var row in tracesResults)
                    data.Add(PostgresBindings.GetTraceItem(row));
            }

            return new SearchResults
            {
                Data = data
            };
        }

        /// <inheritdoc />
        public async Task<KeyValue[]> GetMetadatasAsync(string environment, string groupName)
        {
            var results = await Dal.GetMetadataByGroup(environment, groupName).ConfigureAwait(false);
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
            return dict.Values.OrderBy(i => i.Key).ToArray();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public async Task<List<NodeStatusItem>> GetCurrentStatusAsync(string environment, string machine, string application)
        {
            var results = await Dal.GetStatuses(environment, machine, application).ConfigureAwait(false);
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

            var rData = data.GroupBy(i => new { i.Environment, i.Machine, i.Application })
                .Select(i => i.First())
                .ToList();

            var ids = rData.Select(i => i.InstanceId).ToArray();
            var valuesResults = await Dal.GetStatusesValues(ids).ConfigureAwait(false);

            NodeStatusItem currentNode = null;
            foreach (var item in valuesResults)
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

        /// <inheritdoc />
        public async Task<List<NodeCountersQueryItem>> GetCountersAsync(string environment)
        {
            var results = await Dal.GetCounters(environment).ConfigureAwait(false);
            var data = new List<NodeCountersQueryItem>();
            foreach (var item in results)
                data.Add(PostgresBindings.GetCounterItem(item));
            return data;
        }

        /// <inheritdoc />
        public async Task<NodeCountersQueryItem> GetCounterAsync(Guid counterId)
        {
            var results = await Dal.GetCounter(counterId).ConfigureAwait(false);
            if (results.Count == 0) return null;
            return PostgresBindings.GetCounterItem(results[0]);
        }

        /// <inheritdoc />
        public async Task<List<NodeCountersQueryValue>> GetCounterValuesAsync(Guid counterId, DateTime fromDate, DateTime toDate, int limit = 3600)
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

        /// <inheritdoc />
        public async Task<List<NodeLastCountersValue>> GetLastCounterValuesAsync(Guid counterId, CounterValuesDivision valuesDivision, int samples = 250, DateTime? lastDate = null)
        {
            var counterDataTask = GetCounterAsync(counterId);
            var toDate = Core.Now.AddDays(1).Date;
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
                    if (samples == 0) samples = 168;
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


        /// <inheritdoc />
        public async Task<CounterValuesAggregate> GetCounterAggregationAsync(Guid counterId, DateTime fromDate, DateTime toDate, CounterValuesDataUnit dataUnit)
        {
            var counterTask = GetCounterAsync(counterId);
            var counterValuesTask = Dal.GetCountersValues(counterId, fromDate, toDate);

            var results = new CounterValuesAggregate
            {
                DataUnit = dataUnit,
                FromDate = fromDate,
                ToDate = toDate
            };

            #region Create date placeholders
            if (dataUnit == CounterValuesDataUnit.All)
            {
                results.Data.Add(new CounterValuesAggregateItem { From = fromDate, To = toDate });
            }
            else
            {
                var nToDate = toDate;
                var now = Core.Now;
                if (now < nToDate)
                {
                    nToDate = now.TruncateTo(TimeSpan.FromMinutes(1));
                    switch (dataUnit)
                    {
                        case CounterValuesDataUnit.Yearly:
                            nToDate = nToDate.AddYears(1).Date;
                            break;
                        case CounterValuesDataUnit.Monthly:
                            nToDate = nToDate.AddMonths(1).Date;
                            break;
                        case CounterValuesDataUnit.Daily:
                            nToDate = nToDate.AddDays(1).Date;
                            break;
                        case CounterValuesDataUnit.Hourly:
                            nToDate = nToDate.AddHours(1).TruncateTo(TimeSpan.FromHours(1));
                            break;
                        case CounterValuesDataUnit.Minutely:
                            nToDate = nToDate.AddMinutes(1).TruncateTo(TimeSpan.FromMinutes(1));
                            break;
                    }
                }

                for (var fDate = fromDate.Date; fDate < nToDate;)
                {
                    var count = results.Data.Count;
                    var previous = count > 0 ? results.Data[count - 1] : null;
                    if (previous != null)
                        previous.To = fDate;

                    var current = new CounterValuesAggregateItem { From = fDate };
                    results.Data.Add(current);

                    switch (dataUnit)
                    {
                        case CounterValuesDataUnit.Yearly:
                            fDate = fDate.AddYears(1);
                            break;
                        case CounterValuesDataUnit.Monthly:
                            fDate = fDate.AddMonths(1);
                            break;
                        case CounterValuesDataUnit.Daily:
                            fDate = fDate.AddDays(1);
                            break;
                        case CounterValuesDataUnit.Hourly:
                            fDate = fDate.AddHours(1);
                            break;
                        case CounterValuesDataUnit.Minutely:
                            fDate = fDate.AddMinutes(1);
                            break;
                        default:
                            fDate = fDate.Add(nToDate - fDate).AddSeconds(1);
                            break;
                    }
                }
                if (results.Data.Count > 0)
                    results.Data[results.Data.Count - 1].To = nToDate;
            }
            #endregion

            var counter = await counterTask.ConfigureAwait(false);
            results.Counter = counter;

            var counterValues = await counterValuesTask.ConfigureAwait(false);
            var allValues = new List<float>();
            foreach (var row in counterValues)
            {
                var timestamp = row.Get<DateTime>("timestamp");
                var value = row.Get<float>("value");

                var placeHolder = results.Data.Find(item => item.From <= timestamp && timestamp < item.To);
                if (placeHolder is null) continue;

                if (!(placeHolder.Value is List<float> lFloat))
                {
                    lFloat = new List<float>();
                    placeHolder.Value = lFloat;
                }

                lFloat.Add(value);
                allValues.Add(value);
            }

            foreach (var item in results.Data)
            {
                if (item.Value == null)
                    item.Value = 0;
                else if (item.Value is List<float> lFloat)
                {
                    switch (counter.Type)
                    {
                        case Counters.CounterType.Average:
                            item.Value = lFloat.Count > 0 ? lFloat.Average() : 0;
                            break;
                        case Counters.CounterType.Cumulative:
                            item.Value = lFloat.Sum();
                            break;
                        case Counters.CounterType.Current:
                            item.Value = lFloat.LastOrDefault();
                            break;
                    }
                }
            }

            switch (counter.Type)
            {
                case Counters.CounterType.Average:
                    results.Value = allValues.Count > 0 ? allValues.Average() : 0;
                    break;
                case Counters.CounterType.Cumulative:
                    results.Value = allValues.Sum();
                    break;
                case Counters.CounterType.Current:
                    results.Value = allValues.LastOrDefault();
                    break;
            }

            return results;
        }
    }
}
