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
    /// Trace entity
    /// </summary>
    public class EntTrace : DiagnosticsEntity
    {
        /// <summary>
        /// Trace id
        /// </summary>
        public Guid TraceId { get; set; }
        /// <summary>
        /// Trace tags
        /// </summary>
        public string Tags { get; set; }
        /// <summary>
        /// Trace name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Trace formats
        /// </summary>
        public string[] Formats { get; set; }
    }
}