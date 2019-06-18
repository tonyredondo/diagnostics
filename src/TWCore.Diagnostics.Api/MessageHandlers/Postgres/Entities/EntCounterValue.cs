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
// ReSharper disable UnusedMember.Global

namespace TWCore.Diagnostics.Api.MessageHandlers.Postgres.Entities
{
    /// <summary>
    /// Counter value
    /// </summary>
    public class EntCounterValue
    {
        /// <summary>
        /// Counter id
        /// </summary>
        public Guid CounterId { get; set; }
        /// <summary>
        /// Counter timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Counter value
        /// </summary>
        public double Value { get; set; }
    }
}