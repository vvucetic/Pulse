﻿using Pulse.Core.Common;
using Pulse.Core.Log;
using Pulse.Core.Server.Processes;
using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    public class BackgroundProcessingServer : IBackgroundProcess, IDisposable
    {
        private readonly BackgroundProcessingServerOptions _options;
        private readonly List<IBackgroundProcess> _processes = new List<IBackgroundProcess>();
        private readonly ILog _logger = LogProvider.GetLogger();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly Task _bootstrapTask;

        public BackgroundProcessingServer(DataStorage storage, BackgroundProcessingServerOptions options, IDictionary<string, object> properties, IEnumerable<IBackgroundProcess> processes)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (processes == null) throw new ArgumentNullException(nameof(processes));
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _options = options;
            _processes.AddRange(processes);
            _processes.AddRange(GetRequiredProcesses());

            var serverContext = GetServerContext(properties);
            var context = new BackgroundProcessContext(
                serverId: _options.ServerName,
                cancellationToken: _cts.Token,
                storage: storage,
                properties: properties, 
                serverContext: serverContext
                );
            _bootstrapTask = WrapProcess(this).CreateTask(context);
        }
        private IEnumerable<IBackgroundProcess> GetRequiredProcesses()
        {
            yield return new ServerHeartbeatProcess(_options.HeartbeatInterval);
        }
        public void SendStop()  
        {
            _cts.Cancel();
        }

        private static IBackgroundProcess WrapProcess(IBackgroundProcess process)
        {
            return new InfiniteLoopProcess(new AutomaticRetryProcess(process));
        }

        public void Execute(BackgroundProcessContext context)
        {
            
            context.Storage.HeartbeatServer(context.ServerId, JobHelper.ToJson(context.ServerContext));

            try
            {
                var tasks = _processes
                    .Select(WrapProcess)
                    .Select(process => process.CreateTask(context))
                    .ToArray();

                Task.WaitAll(tasks);
            }
            finally
            {
                //using (var connection = context.Storage.GetConnection())
                //{
                //    connection.RemoveServer(context.ServerId);
                //}
            }
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        private static ServerContext GetServerContext(IDictionary<string, object> properties)
        {
            var serverContext = new ServerContext();

            if (properties.ContainsKey("Queues"))
            {
                var array = properties["Queues"] as string[];
                if (array != null)
                {
                    serverContext.Queues = array;
                }
            }

            if (properties.ContainsKey("WorkerCount"))
            {
                serverContext.WorkerCount = (int)properties["WorkerCount"];
            }

            serverContext.ServerStartedAt = DateTime.UtcNow;
            return serverContext;
        }

        public void Dispose()
        {
            SendStop();

            // TODO: Dispose _cts
            
            if (!_bootstrapTask.Wait(_options.ShutdownTimeout))
            {
                _logger.Log(LogLevel.Warning, "Processing server takes too long to shutdown. Performing ungraceful shutdown.");
            }
        }
    }
}
