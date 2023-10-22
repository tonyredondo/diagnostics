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
using System.Collections;
using System.Threading.Tasks;
using TWCore.Diagnostics.Api.MessageHandlers.Postgres;
using TWCore.Diagnostics.Api.MessageHandlers.RavenDb;
using TWCore.Services;

// ReSharper disable ClassNeverInstantiated.Global

namespace TWCore.Diagnostics.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Core.InitDefaults(false);

            var enableMessaging = true;
            if (Core.Settings.TryGet("Diagnostics.Messaging.Enabled", out var messagingSettings))
                enableMessaging = messagingSettings.Value.ParseTo(false);
            else
                enableMessaging = false;

            Core.RunOnInit(() => Core.Log.InfoBasic("Diagnostics.Messaging.Enabled is {0}", enableMessaging));
            // CreateDBOnStartUp().WaitAsync();

            if (enableMessaging)
                Core.RunService(() => new ServiceList(WebService.CreateHost<Startup>(), new DiagnosticRawMessagingServiceAsync(), new DiagnosticBotService()), args);
            else
                Core.RunService(() => new ServiceList(WebService.CreateHost<Startup>(), new DiagnosticBotService()), args);
        }

        private static async Task CreateDBOnStartUp()
        {
            Core.Log.InfoBasic("Creating database...");
            var dal = new PostgresDal();
            try
            {
                await dal.CreateDatabaseAsync().ConfigureAwait(false);
            }
            catch { }
            await dal.EnsureTablesAndIndexesAsync().ConfigureAwait(false);
            Core.Log.InfoBasic("Done.");
        }
    }
}