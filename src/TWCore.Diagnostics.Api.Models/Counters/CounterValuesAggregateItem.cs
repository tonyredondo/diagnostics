﻿/*
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
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace TWCore.Diagnostics.Api.Models.Counters
{
    [DataContract]
    public class CounterValuesAggregateItem
    {
        [XmlAttribute, DataMember]
        public DateTime From { get; set; }
        [XmlAttribute, DataMember]
        public DateTime To { get; set; }
        [XmlAttribute, DataMember]
        public long Timestamp 
            => (long)(From.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds + 0.5);
        [XmlElement, DataMember]
        public object Value { get; set; }
    }
}
