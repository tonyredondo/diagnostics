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
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace TWCore.Diagnostics.Api.Models.Counters
{
    [DataContract]
    public class CounterValuesAggregate
    {
        [XmlAttribute, DataMember]
        public NodeCountersQueryItem Counter { get; set; }
        [XmlAttribute, DataMember]
        public DateTime FromDate { get; set; }
        [XmlAttribute, DataMember]
        public DateTime ToDate { get; set; }
        [XmlAttribute, DataMember]
        public CounterValuesDataUnit DataUnit { get; set; }
        [XmlElement, DataMember]
        public object Value { get; set; }
        [XmlAttribute, DataMember]
        public List<CounterValuesAggregateItem> Data { get; set; } = new List<CounterValuesAggregateItem>();
    }
}
