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
using TWCore.Diagnostics.Log;

namespace TWCore.Diagnostics.Api.MessageHandlers.RavenDb.Indexes
{
    public class Metadata_Search : AbstractIndexCreationTask<GroupMetadata, Metadata_Search.Result>
    {
        public class Result
        {
            public string GroupName { get; set; }
            public string MetaValue { get; set; }
        }
        public Metadata_Search()
        {
            Map = metadata => from meta in metadata
                              from itemValue in meta.Items
                            select new
                            {
                                GroupName = meta.GroupName,
                                MetaValue = itemValue.Value
                            };

            Index(x => x.GroupName, FieldIndexing.Search);
            Index(x => x.MetaValue, FieldIndexing.Search);
        }
    }
}