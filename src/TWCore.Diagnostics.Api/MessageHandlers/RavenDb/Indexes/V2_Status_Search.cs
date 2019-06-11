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
using TWCore.Diagnostics.Api.Models.Status;

namespace TWCore.Diagnostics.Api.MessageHandlers.RavenDb.Indexes
{
    public class V2_Status_Search : AbstractIndexCreationTask<NodeStatusItem>
    {
        public V2_Status_Search()
        {
            Map = statuses => from status in statuses
                              where status.Environment != null
                              orderby status.Timestamp descending
                              select new
                              {
                                  status.Environment,
                                  status.Timestamp,
                                  status.Machine,
                                  status.Application
                              };

            Index(i => i.Environment, FieldIndexing.Exact);
            Index(i => i.Timestamp, FieldIndexing.Default);
            Index(i => i.Machine, FieldIndexing.Exact);
            Index(i => i.Application, FieldIndexing.Exact);
        }
    }
}