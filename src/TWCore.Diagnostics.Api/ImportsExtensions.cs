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

using System.Threading.Tasks;
using TWCore.Diagnostics.Api.MessageHandlers;
using TWCore.Services;

// ReSharper disable ClassNeverInstantiated.Global

namespace TWCore.Diagnostics.Api
{
    public class ImportLogsExtensions : ContainerParameterServiceAsync
    {
        public ImportLogsExtensions() : base("import-logs", "Import logs from the RavenDB to Postgresql") { }

        protected override async Task OnHandlerAsync(ParameterHandlerInfo info)
        {
            Core.Log.InfoBasic("Starting...");
            await Imports.ImportLogsAsync().ConfigureAwait(false);
            info.ShouldEndExecution = true;
            Core.Log.InfoBasic("Done.");
        }
    }
    public class ImportMetadataExtensions : ContainerParameterServiceAsync
    {
        public ImportMetadataExtensions() : base("import-metadata", "Import metadata from the RavenDB to Postgresql") { }

        protected override async Task OnHandlerAsync(ParameterHandlerInfo info)
        {
            Core.Log.InfoBasic("Starting...");
            await Imports.ImportMetadataAsync().ConfigureAwait(false);
            info.ShouldEndExecution = true;
            Core.Log.InfoBasic("Done.");
        }
    }
    public class ImportTracesExtensions : ContainerParameterServiceAsync
    {
        public ImportTracesExtensions() : base("import-traces", "Import traces from the RavenDB to Postgresql") { }

        protected override async Task OnHandlerAsync(ParameterHandlerInfo info)
        {
            Core.Log.InfoBasic("Starting...");
            await Imports.ImportTracesAsync().ConfigureAwait(false);
            info.ShouldEndExecution = true;
            Core.Log.InfoBasic("Done.");
        }
    }

    public class ImportCountersExtensions : ContainerParameterServiceAsync
    {
        public ImportCountersExtensions() : base("import-counters", "Import counters from the RavenDB to Postgresql") { }

        protected override async Task OnHandlerAsync(ParameterHandlerInfo info)
        {
            Core.Log.InfoBasic("Starting...");
            await Imports.ImportCounterAsync().ConfigureAwait(false);
            await Imports.ImportCounterValuesAsync().ConfigureAwait(false);
            info.ShouldEndExecution = true;
            Core.Log.InfoBasic("Done.");
        }
    }

    public class ImportStatusesExtensions : ContainerParameterServiceAsync
    {
        public ImportStatusesExtensions() : base("import-statuses", "Import statuses from the RavenDB to Postgresql") { }

        protected override async Task OnHandlerAsync(ParameterHandlerInfo info)
        {
            Core.Log.InfoBasic("Starting...");
            await Imports.ImportStatusesAsync().ConfigureAwait(false);
            info.ShouldEndExecution = true;
            Core.Log.InfoBasic("Done.");
        }
    }
}