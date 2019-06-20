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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TWCore.Bot;
using TWCore.Diagnostics.Api.MessageHandlers;
using TWCore.Diagnostics.Api.MessageHandlers.Postgres;
using TWCore.Diagnostics.Api.MessageHandlers.RavenDb;
using TWCore.Diagnostics.Api.Models.Log;
using TWCore.Diagnostics.Counters;
using TWCore.Diagnostics.Log;
using TWCore.Diagnostics.Status;
using TWCore.Diagnostics.Trace.Storages;
using TWCore.Messaging.RawServer;
using TWCore.Serialization;
using TWCore.Services;
using TWCore.Services.Messaging;
// ReSharper disable UnusedMember.Global

namespace TWCore.Diagnostics.Api
{
    public class DiagnosticRawMessagingServiceAsync : RawMessagingServiceAsync
    {
        private static readonly DiagnosticsSettings Settings = Core.GetSettings<DiagnosticsSettings>();
        private readonly BlockingCollection<LogItem> _logItems = new BlockingCollection<LogItem>();
        private readonly BlockingCollection<GroupMetadata> _groupsMetadata = new BlockingCollection<GroupMetadata>();
        private readonly BlockingCollection<MessagingTraceItem> _messagingTraceItems = new BlockingCollection<MessagingTraceItem>();
        private readonly BlockingCollection<ICounterItem> _counterItems = new BlockingCollection<ICounterItem>();
        private Timer _processTimer;
        private int _inProcess = 0;
        
        protected override void OnInit(string[] args)
        {
            Core.GlobalSettings.LargeObjectHeapCompactTimeoutInMinutes = 1;
            Core.GlobalSettings.ReloadSettings();
            SerializerManager.SupressFileExtensionWarning = true;
            base.OnInit(args);

            DbHandlers.Instance.Messages.Init();

            ImportLogsAsync().WaitAsync();

            var pDal = new PostgresDal();

            var fromDate = DateTime.Today.AddDays(-7);
            var fromDate2 = DateTime.Today.AddMonths(-5);
            var toDate = DateTime.Now;
            var environments = pDal.GetAllEnvironments().WaitAndResults();
            var levels = pDal.GetLogLevelsByEnvironment("Docker", fromDate, toDate).WaitAndResults();
            var logs = pDal.GetLogsByApplication("Docker", "Agsw.Travel.Flights.Providers.Services.Travelfusion", LogLevel.Error, fromDate2, toDate, 1, 25).WaitAndResults();
            var logsByGroup = pDal.GetLogsByGroup("Docker", "e9487fd1-3b5f-4eec-87a2-691207b1ed53", fromDate2, toDate).WaitAndResults();
            var logsSearch = pDal.SearchLogs("Docker", "e9487fd1-3b5f-4eec-87a2-691207b1ed53", fromDate2, toDate).WaitAndResults();

            var processTimerTimeSpan = TimeSpan.FromSeconds(Settings.ProcessTimerInSeconds);
            _processTimer = new Timer(ProcessItems, null, processTimerTimeSpan, processTimerTimeSpan);
        }

        private static Task ImportLogsAsync()
        {
            return RavenHelper.ExecuteAsync(async session =>
            {
                var pDal = new PostgresDal();

                var query = session.Advanced.AsyncDocumentQuery<NodeLogItem>();
                var index = 0;
                var enumerator = await session.Advanced.StreamAsync(query).ConfigureAwait(false);

                var insertBuffer = new List<MessageHandlers.Postgres.Entities.EntLog>();
                Console.WriteLine("Importing logs...");
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var item = enumerator.Current.Document;
                    index++;
                    if (index % 1000 == 0)
                        Console.WriteLine("Writing: " + index);

                    insertBuffer.Add(new MessageHandlers.Postgres.Entities.EntLog
                    {
                        LogId = item.LogId,
                        Environment = item.Environment,
                        Machine = item.Machine,
                        Application = item.Application,
                        Assembly = item.Assembly,
                        Type = item.Type,
                        Code = item.Code,
                        Group = item.Group,
                        Level = item.Level,
                        Timestamp = item.Timestamp,
                        Message = item.Message,
                        Exception = item.Exception
                    });

                    if (insertBuffer.Count == 500)
                    {
                        Console.WriteLine("Saving...");
                        try
                        {
                            await pDal.InsertLogAsync(insertBuffer, true).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        insertBuffer.Clear();
                    }
                }
                if (insertBuffer.Count > 0)
                {
                    try
                    {
                        await pDal.InsertLogAsync(insertBuffer, true).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    insertBuffer.Clear();
                }
                Console.WriteLine("Total Items: " + index);
            });
        }

        private void ProcessItems(object state)
        {
            if (Interlocked.CompareExchange(ref _inProcess, 1, 0) == 1) return;
            try
            {
                var lstLogs = new List<LogItem>();
                while (_logItems.TryTake(out var item, 100))
                    lstLogs.Add(item);
                var logTask = lstLogs.Count > 0 ? DbHandlers.Instance.Messages.ProcessLogItemsMessageAsync(lstLogs) : Task.CompletedTask;

                var lstGroupsMeta = new List<GroupMetadata>();
                while (_groupsMetadata.TryTake(out var item, 100))
                    lstGroupsMeta.Add(item);
                var groupTask = lstGroupsMeta.Count > 0 ? DbHandlers.Instance.Messages.ProcessGroupMetadataMessageAsync(lstGroupsMeta) : Task.CompletedTask;

                var lstTraces = new List<MessagingTraceItem>();
                while (_messagingTraceItems.TryTake(out var item, 100))
                    lstTraces.Add(item);
                var traceTask = lstTraces.Count > 0 ? DbHandlers.Instance.Messages.ProcessTraceItemsMessageAsync(lstTraces) : Task.CompletedTask;

                var lstCounters = new List<ICounterItem>();
                while (_counterItems.TryTake(out var item, 100))
                    lstCounters.Add(item);
                var counterTask = lstCounters.Count > 0 ? DbHandlers.Instance.Messages.ProcessCountersMessageAsync(lstCounters) : Task.CompletedTask;


                Task.WaitAll(logTask, groupTask, traceTask, counterTask);
            }
            finally
            {
                Interlocked.Exchange(ref _inProcess, 0);
            }
        }
        
        #region Overrides
        /// <inheritdoc />
        /// <summary>
        /// Gets the message processor
        /// </summary>
        /// <param name="server">Queue server object instance</param>
        /// <returns>Message processor instance</returns>
        protected override IMessageProcessorAsync GetMessageProcessorAsync(IMQueueRawServer server)
        {
            var processor = new ActionMessageProcessorAsync(Settings.ParallelMessagingProcess);
            processor.RegisterAction<RawRequestReceivedEventArgs>(async (message, cancellationToken) =>
            {
                var rcvMessage = server.ReceiverSerializer.Deserialize<object>(message.Request);
                if (rcvMessage is List<LogItem> msgLogs)
                {
                    if (msgLogs.Count == 0) return;
                    foreach(var item in msgLogs)
                        _logItems.Add(item);
                }
                else if (rcvMessage is List<GroupMetadata> msgGroups)
                {
                    if (msgGroups.Count == 0) return;
                    foreach (var item in msgGroups)
                        _groupsMetadata.Add(item);
                }
                else if (rcvMessage is List<MessagingTraceItem> msgTraces)
                {
                    if (msgTraces.Count == 0) return;
                    foreach (var item in msgTraces)
                        _messagingTraceItems.Add(item);
                }
                else if (rcvMessage is StatusItemCollection msgStatus)
                {
                    await DbHandlers.Instance.Messages.ProcessStatusMessageAsync(msgStatus).ConfigureAwait(false);
                }
                else if (rcvMessage is List<ICounterItem> msgCounters)
                {
                    if (msgCounters.Count == 0) return;
                    foreach (var item in msgCounters)
                        _counterItems.Add(item);
                }
            });
            return processor;
        }
        /// <inheritdoc />
        /// <summary>
        /// Gets the queue server object
        /// </summary>
        /// <returns>IMQueueServer object instance</returns>
        protected override IMQueueRawServer GetQueueServer()
            => Core.Services.GetQueueRawServer();
        #endregion
    }
}