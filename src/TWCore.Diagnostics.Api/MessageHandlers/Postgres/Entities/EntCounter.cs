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
using TWCore.Diagnostics.Counters;

// ReSharper disable UnusedMember.Global

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres.Entities
{
    /// <summary>
    /// Counter definition
    /// </summary>
    public class EntCounter : ApplicationEntity
    {
        /// <summary>
        /// Counter identifier
        /// </summary>
        public Guid CounterId { get; set; }
        /// <summary>
        /// Counter category
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// Counter name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Counter type
        /// </summary>
        public CounterType Type { get; set; }
        /// <summary>
        /// Counter level
        /// </summary>
        public CounterLevel Level { get; set; }
        /// <summary>
        /// Counter kind
        /// </summary>
        public CounterKind Kind { get; set; }
        /// <summary>
        /// Counter unit
        /// </summary>
        public CounterUnit Unit { get; set; }
        /// <summary>
        /// Counter type of the value
        /// </summary>
        public string TypeOfValue { get; set; }
    }
}