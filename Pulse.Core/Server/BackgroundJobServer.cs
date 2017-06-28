using Pulse.Core.Log;
using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
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

        public BackgroundJobServer(BackgroundJobServerOptions options, DataStorage storage, IEnumerable<IBackgroundProcess> additionalProcesses)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (additionalProcesses == null) throw new ArgumentNullException(nameof(additionalProcesses));

            this._options = options;
            this._storage = storage;
            var processes = new List<IBackgroundProcess>();
            processes.AddRange(GetRequiredProcesses());
            processes.AddRange(additionalProcesses);

            var properties = new Dictionary<string, object>
            {
                { "Queues", options.Queues },
                { "WorkerCount", options.WorkerCount }
            };

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
                GetProcessingServerOptions(),
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

        private IEnumerable<IBackgroundProcess> GetRequiredProcesses()
        {
            var processes = new List<IBackgroundProcess>();

            //var filterProvider = _options.FilterProvider ?? JobFilterProviders.Providers;

            //var factory = new BackgroundJobFactory(filterProvider);
            var performer = new CoreBackgroundJobPerformer(_options.Activator ?? JobActivator.Current);
            //var stateChanger = new BackgroundJobStateChanger(filterProvider);

            for (var i = 0; i < _options.WorkerCount; i++)
            {
                processes.Add(new Worker(this._options.Queues, performer, this._storage));
            }

            processes.Add(new DelayedJobScheduler(_options.SchedulePollingInterval, _storage));
            //processes.Add(new RecurringJobScheduler(factory));

            return processes;
        }

        private BackgroundProcessingServerOptions GetProcessingServerOptions()
        {
            return new BackgroundProcessingServerOptions
            {
                ShutdownTimeout = _options.ShutdownTimeout,
                HeartbeatInterval = _options.HeartbeatInterval,
                //ServerCheckInterval = _options.ServerWatchdogOptions?.CheckInterval ?? _options.ServerCheckInterval,
                //ServerTimeout = _options.ServerWatchdogOptions?.ServerTimeout ?? _options.ServerTimeout,
                ServerName = _options.ServerName
            };
        }
    }
}
