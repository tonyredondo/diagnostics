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
    /// Status entity
    /// </summary>
    public class EntStatus : ApplicationEntity
    {
        /// <summary>
        /// Status id
        /// </summary>
        public Guid StatusId { get; set; }
        /// <summary>
        /// Machine name
        /// </summary>
        public string Machine { get; set; }
        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Application display name
        /// </summary>
        public string ApplicationDisplay { get; set; }
        /// <summary>
        /// Elapsed time
        /// </summary>
        public double Elapsed { get; set; }
        /// <summary>
        /// Start time
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// Absolute date
        /// </summary>
        public DateTime Date { get; set; }
    }
}