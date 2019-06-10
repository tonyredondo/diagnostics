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
using System.Linq;
using Raven.Client.Documents.Indexes;
using TWCore.Diagnostics.Log;
using TWCore.Diagnostics.Api.Models.Log;
using TWCore.Diagnostics.Api.Models.Trace;

namespace TWCore.Diagnostics.Api.MessageHandlers.RavenDb.Indexes
{
    public class V2_Diagnostics_Search : AbstractMultiMapIndexCreationTask<V2_Diagnostics_Search.Result>
    {
        public class Result
        {
            public string Environment { get; set; }
            public DateTime Timestamp { get; set; }
            public string Group { get; set; }
            public string SearchTerm { get; set; }
        }

        public V2_Diagnostics_Search()
        {
            AddMap<NodeLogItem>(logs => from log in logs 
                                        select new 
                                        { 
                                            Environment = log.Environment, 
                                            Timestamp = log.Timestamp, 
                                            Group = log.Group, 
                                            SearchTerm = log.Message
                                        });

            AddMap<NodeTraceItem>(traces => from trace in traces 
                                            select new 
                                            { 
                                                Environment = trace.Environment, 
                                                Timestamp = trace.Timestamp, 
                                                Group = trace.Group, 
                                                SearchTerm = trace.Name
                                            });

            AddMap<GroupMetadata>(metadata => from meta in metadata 
                                              from itemValue in meta.Items
                                              select new 
                                              {
                                                  Environment = (string) null,
                                                  Timestamp = meta.Timestamp,
                                                  Group = meta.GroupName,
                                                  SearchTerm = itemValue.Key + "=" + itemValue.Value
                                              });

            Index(x => x.Environment, FieldIndexing.Exact);
            Index(x => x.Timestamp, FieldIndexing.Default);
            Index(x => x.Group, FieldIndexing.Search);
            Index(x => x.SearchTerm, FieldIndexing.Search);
        }
    }
}