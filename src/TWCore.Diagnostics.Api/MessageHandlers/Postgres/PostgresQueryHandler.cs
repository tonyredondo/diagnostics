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
using System.Threading.Tasks;
using TWCore.Collections;
using TWCore.Compression;
using TWCore.Diagnostics.Api.Models;
using TWCore.Diagnostics.Api.Models.Counters;
using TWCore.Diagnostics.Api.Models.Log;
using TWCore.Diagnostics.Api.Models.Status;
using TWCore.Diagnostics.Api.Models.Trace;
using TWCore.Diagnostics.Log;
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
                var valTimestamp = value.Get<DateTime>("timestamp");
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


                var levelSeries = levelTimes.Series.FirstOrDefault(ls => ls.Date == valTimestamp);
                if (levelSeries == null)
                {
                    levelSeries = new TimeCount
                    {
                        Date = valTimestamp
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

            foreach(var row in results)
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
            foreach(var row in results)
            {
                var item = GetTraceItem(row);
                data.Add(item);
            }
            return data;
        }


        public Task<SerializedObject> GetTraceObjectAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTraceXmlAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTraceJsonAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTraceTxtAsync(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<SearchResults> SearchAsync(string environment, string searchTerm, DateTime fromDate, DateTime toDate)
        {
            var results = await Dal.Search(environment, searchTerm, fromDate, toDate, 10).ConfigureAwait(false);
            var groups = results.Select(row => (string)row[0]).ToList();
            var data = new List<NodeInfo>();

            foreach(var group in groups)
            {
                var logsResults = await Dal.GetLogsByGroup(environment, group, fromDate, toDate).ConfigureAwait(false);
                var tracesResults = await Dal.GetTracesByGroupId(environment, group).ConfigureAwait(false);

                foreach(var row in logsResults)
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
            foreach(var row in results)
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

        public Task<PagedList<NodeStatusItem>> GetStatusesAsync(string environment, string machine, string application, DateTime fromDate, DateTime toDate, int page, int pageSize = 50)
        {
            throw new NotImplementedException();
        }

        public Task<List<NodeStatusItem>> GetCurrentStatus(string environment, string machine, string application)
        {
            throw new NotImplementedException();
        }

        public Task<List<NodeCountersQueryItem>> GetCounters(string environment)
        {
            throw new NotImplementedException();
        }

        public Task<NodeCountersQueryItem> GetCounter(Guid counterId)
        {
            throw new NotImplementedException();
        }

        public Task<List<NodeCountersQueryValue>> GetCounterValues(Guid counterId, DateTime fromDate, DateTime toDate, int limit = 3600)
        {
            throw new NotImplementedException();
        }

        public Task<List<NodeLastCountersValue>> GetLastCounterValues(Guid counterId, CounterValuesDivision valuesDivision, int samples = 250, DateTime? lastDate = null)
        {
            throw new NotImplementedException();
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

    }
}
