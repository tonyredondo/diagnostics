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

using System.Linq;
using Raven.Client.Documents.Indexes;
using TWCore.Diagnostics.Api.Models.Trace;

namespace TWCore.Diagnostics.Api.MessageHandlers.RavenDb.Indexes
{
    public class V2_Traces_ByGroup : AbstractIndexCreationTask<NodeTraceItem>
    {
        public V2_Traces_ByGroup()
        {
            Map = traces => from trace in traces
                            where trace.Environment != null
                            select new
                            {
                                trace.Environment,
                                trace.Group,
                                trace.Timestamp
                            };

            Index(t => t.Environment, FieldIndexing.Exact);
            Index(t => t.Group, FieldIndexing.Exact);
        }
    }
}