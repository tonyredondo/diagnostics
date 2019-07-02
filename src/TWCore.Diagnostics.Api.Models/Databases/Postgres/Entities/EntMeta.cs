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

// ReSharper disable UnusedMember.Global
using System;

namespace TWCore.Diagnostics.Api.Models.Database.Postgres.Entities
{
    /// <summary>
    /// Metadata entity
    /// </summary>
    public class EntMeta
    {
        /// <summary>
        /// Environment
        /// </summary>
        public string Environment { get; set; }
        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Date
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// Diagnostics group name
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// Metadata Key
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Metadata Value
        /// </summary>
        public string Value { get; set; }
    }
}