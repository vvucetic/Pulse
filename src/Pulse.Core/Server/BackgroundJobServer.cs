using Pulse.Core.Common;
using Pulse.Core.Log;
using Pulse.Core.Server.Processes;
using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Pulse.Core.Server
{
    public class BackgroundJobServer : IDisposable
    {
        private readonly BackgroundJobServerOptions _options;
        private readonly DataStorage _storage;
        private readonly BackgroundProcessingServer _processingServer;
        private readonly ILog _logger = LogProvider.GetLogger();


        public BackgroundJobServer():this(new BackgroundJobServerOptions(), DataStorage.Current, new List<IBackgroundProcess>())
        {

        }

        public BackgroundJobServer(BackgroundJobServerOptions options) : this(options, DataStorage.Current, new List<IBackgroundProcess>())
        {

        }

        public BackgroundJobServer(BackgroundJobServerOptions options, DataStorage storage, IEnumerable<IBackgroundProcess> additionalProcesses)
        {
            if (additionalProcesses == null) throw new ArgumentNullException(nameof(additionalProcesses));

            this._options = options ?? throw new ArgumentNullException(nameof(options));
            this._storage = storage ?? throw new ArgumentNullException(nameof(storage));

            var properties = new Dictionary<string, object>
            {
                { "Queues", options.Queues },
                { "WorkerCount", options.WorkerCount }
            };
            var processingServerOptions = GetProcessingServerOptions(properties);

            storage.HeartbeatServer(processingServerOptions.ServerName, JobHelper.ToJson(processingServerOptions.ServerContext));
            
            var processes = new List<IBackgroundProcess>();
            processes.AddRange(GetRequiredProcesses(processingServerOptions.ServerName));
            processes.AddRange(additionalProcesses);

            _logger.Log("Starting Pulse Server");
            _logger.Log($"Using job storage: '{storage}'");

            storage.WriteOptionsToLog(_logger);

            _logger.Log("Using the following options for Pulse Server:");
            _logger.Log($"    Worker count: {options.WorkerCount}");
            _logger.Log($"    Listening queues: {String.Join(", ", options.Queues.Select(x => "'" + x + "'"))}");
            _logger.Log($"    Shutdown timeout: {options.ShutdownTimeout}");
            _logger.Log($"    Schedule polling interval: {options.SchedulePollingInterval}");

            _processingServer = new BackgroundProcessingServer(
                storage,
                processingServerOptions,
                properties,
                processes
                );
        }

        public void SendStop()
        {
            _logger.Log("Pulse Server is stopping...");
            _processingServer.SendStop();
        }

        public void Dispose()
        {
            _processingServer.Dispose();
            _logger.Log("Pulse Server stopped.");
            
        }

        private IEnumerable<IBackgroundProcess> GetRequiredProcesses(string serverId)
        {
            var processes = new List<IBackgroundProcess>();

            //var filterProvider = _options.FilterProvider ?? JobFilterProviders.Providers;

            //var factory = new BackgroundJobFactory(filterProvider);
            var performer = new CoreBackgroundJobPerformer(_options.Activator ?? JobActivator.Current);
            //var stateChanger = new BackgroundJobStateChanger(filterProvider);

            for (var i = 0; i < _options.WorkerCount; i++)
            {
                processes.Add(new WorkerProcess(this._options.Queues, performer, this._storage, serverId));
            }

            processes.Add(new DelayedJobSchedulerProcess(_options.SchedulePollingInterval, _storage));
            processes.Add(new RecurringTasksProcess());

            return processes;
        }

        private BackgroundProcessingServerOptions GetProcessingServerOptions(Dictionary<string, object> properties)
        {
            return new BackgroundProcessingServerOptions
            {
                ShutdownTimeout = _options.ShutdownTimeout,
                HeartbeatInterval = _options.HeartbeatInterval,
                ServerCheckInterval = _options.ServerCheckInterval,
                ServerTimeout = _options.ServerTimeout,
                ServerName = GetGloballyUniqueServerId(),
                ServerContext = GetServerContext(properties)
            };
        }

        private static ServerContext GetServerContext(IDictionary<string, object> properties)
        {
            var serverContext = new ServerContext();

            if (properties.ContainsKey("Queues"))
            {
                if (properties["Queues"] is string[] array)
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

        private string GetGloballyUniqueServerId()
        {
            var serverName = _options.ServerName
                ?? Environment.GetEnvironmentVariable("COMPUTERNAME")
                ?? Environment.GetEnvironmentVariable("HOSTNAME");

            var guid = Guid.NewGuid().ToString();

            if (!String.IsNullOrWhiteSpace(serverName))
            {
                serverName += ":" + Process.GetCurrentProcess().Id;
            }

            return !String.IsNullOrWhiteSpace(serverName)
                ? $"{serverName.ToLowerInvariant()}:{guid}"
                : guid;
        }
    }
}
