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
using TWCore.Diagnostics.Api.Models.Counters;
using TWCore.Diagnostics.Api.Models.Database.Postgres.Entities;
using TWCore.Diagnostics.Api.Models.Log;
using TWCore.Diagnostics.Api.Models.Trace;
using TWCore.Diagnostics.Counters;
using TWCore.Diagnostics.Log;
using TWCore.Serialization;

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres
{
    public static class PostgresBindings
    {
        public static NodeLogItem GetLogItem(PostgresHelper.DbRow row)
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

        public static NodeTraceItem GetTraceItem(PostgresHelper.DbRow row)
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

        public static NodeCountersQueryItem GetCounterItem(PostgresHelper.DbRow row)
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

        public static NodeCountersItem GetCounter(PostgresHelper.DbRow row)
        {
            return new NodeCountersItem
            {
                Environment = row.Get<string>("environment"),
                Id = row.Get<Guid>("counter_id").ToString(),
                Application = row.Get<string>("application"),
                Category = row.Get<string>("category"),
                CountersId = row.Get<Guid>("counter_id"),
                Name = row.Get<string>("name"),
                TypeOfValue = row.Get<string>("typeofvalue"),
                Kind = row.Get<CounterKind>("kind"),
                Level = row.Get<CounterLevel>("level"),
                Type = row.Get<CounterType>("type"),
                Unit = row.Get<CounterUnit>("unit")
            };
        }

        public static EntCounter GetCounterEntity(PostgresHelper.DbRow row)
        {
            return new EntCounter
            {
                Environment = row.Get<string>("environment"),
                Application = row.Get<string>("application"),
                Category = row.Get<string>("category"),
                CounterId = row.Get<Guid>("counter_id"),
                Name = row.Get<string>("name"),
                TypeOfValue = row.Get<string>("typeofvalue"),
                Kind = row.Get<CounterKind>("kind"),
                Level = row.Get<CounterLevel>("level"),
                Type = row.Get<CounterType>("type"),
                Unit = row.Get<CounterUnit>("unit")
            };
        }
    }
}
