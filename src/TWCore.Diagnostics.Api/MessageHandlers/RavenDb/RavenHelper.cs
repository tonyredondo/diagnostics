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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;
using TWCore.Settings;
using TWCore.Threading;

namespace TWCore.Diagnostics.Api.MessageHandlers.RavenDb
{
    public static class RavenHelper
    {
        public static readonly RavenDbSettings Settings = Core.GetSettings<RavenDbSettings>();

        #region DocumentStore
        private static Lazy<DocumentStore> _documentStoreLazy = new Lazy<DocumentStore>(CreateDocumentStore);
        private static DocumentStore CreateDocumentStore()
        {
            Core.Log.InfoBasic("Creating RavenDB document store");
            var store = new DocumentStore
            {
                Urls = Settings.Urls, 
                Database = Settings.Database
            };
            store.Initialize();

            Core.Log.InfoBasic("Creating RavenDB indexes");
            IndexCreation.CreateIndexes(typeof(RavenHelper).Assembly, store);
            
            return store;
        }
        public static void CloseDocumentStore()
        {
            if (!_documentStoreLazy.IsValueCreated) return;
            _documentStoreLazy.Value.Dispose();
            _documentStoreLazy = new Lazy<DocumentStore>(CreateDocumentStore);
        }
        #endregion

        public static void Init()
        {
            var value = _documentStoreLazy.Value;
        }

        public static async Task ExecuteAsync(Func<IAsyncDocumentSession, Task> sessionFunc, [CallerMemberName]string memberName = null)
        {
            TimeSpan tmSession, tmQuery;
            var sw = Stopwatch.StartNew();
            using (var session = _documentStoreLazy.Value.OpenAsyncSession())
            {
                tmSession = sw.Elapsed;
                await sessionFunc(session).ConfigureAwait(false);
                tmQuery = sw.Elapsed;
            }
            Core.Log.Stats(memberName + " - Database Time: Session: {0:0.000}ms, Query: {1:0.000}ms, Total: {2:0.000}ms",
                tmSession.TotalMilliseconds, tmQuery.TotalMilliseconds - tmSession.TotalMilliseconds, tmQuery.TotalMilliseconds);
        }

        public static async Task<T> ExecuteAndReturnAsync<T>(Func<IAsyncDocumentSession, Task<T>> sessionFunc, [CallerMemberName]string memberName = null)
		{
            TimeSpan tmSession, tmQuery;
            T res;
            var sw = Stopwatch.StartNew();
            using (var session = _documentStoreLazy.Value.OpenAsyncSession())
            {
                tmSession = sw.Elapsed;
                res = await sessionFunc(session).ConfigureAwait(false);
                tmQuery = sw.Elapsed;
            }
            Core.Log.Stats(memberName + " - Database Time: Session: {0:0.000}ms, Query: {1:0.000}ms, Total: {2:0.000}ms", 
                tmSession.TotalMilliseconds, tmQuery.TotalMilliseconds - tmSession.TotalMilliseconds, tmQuery.TotalMilliseconds);
            return res;
		}

        public static async Task BulkInsertAsync(Func<BulkInsertOperation, Task> bulkInsertOperation, [CallerMemberName]string memberName = null)
        {
            TimeSpan tmSession, tmQuery;
            var sw = Stopwatch.StartNew();
            using (var bulkInsert = _documentStoreLazy.Value.BulkInsert())
            {
                tmSession = sw.Elapsed;
                await bulkInsertOperation(bulkInsert).ConfigureAwait(false);
                tmQuery = sw.Elapsed;
            }
            Core.Log.Stats(memberName + " - Database Time: Session: {0:0.000}ms, Query: {1:0.000}ms, Total: {2:0.000}ms",
                tmSession.TotalMilliseconds, tmQuery.TotalMilliseconds - tmSession.TotalMilliseconds, tmQuery.TotalMilliseconds);
        }

        public class RavenDbSettings : SettingsBase
        {
            [SettingsArray(",")]
            public string[] Urls { get; set; }
            public string Database { get; set; } = "Diagnostics";
        }
    }
}