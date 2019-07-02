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
using System.Collections.Generic;
using System.Threading.Tasks;
using TWCore.Diagnostics.Log;
// ReSharper disable UnusedMember.Global

namespace TWCore.Diagnostics.Api.Models.Database.Postgres.Entities
{
    /// <summary>
    /// Log entity
    /// </summary>
    public class EntLog : DiagnosticsEntity
    {
        /// <summary>
        /// Log id
        /// </summary>
        public Guid LogId { get; set; }
        /// <summary>
        /// Date of the log
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// Assembly name
        /// </summary>
        public string Assembly { get; set; }
        /// <summary>
        /// Type name
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Code of message
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// Log level
        /// </summary>
        public LogLevel Level { get; set; }
        /// <summary>
        /// Log Message
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Exception object
        /// </summary>
        public SerializableException Exception { get; set; }
    }
}