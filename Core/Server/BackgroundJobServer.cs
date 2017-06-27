using Pulse.Core.Storage;
using System;
using System.Collections.Generic;

namespace Pulse.Core.Server
{
    public class BackgroundJobServer : IDisposable
    {
        private readonly BackgroundJobServerOptions _options;
        private readonly DataStorage _storage;
        private readonly BackgroundProcessingServer _processingServer;

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

            //TODO Log start

            _processingServer = new BackgroundProcessingServer(
                storage,
                GetProcessingServerOptions(),
                properties,
                processes
                );
        }

        public void SendStop()
        {
            //TODO LOg
            //Logger.Debug("Hangfire Server is stopping...");
            _processingServer.SendStop();
        }

        public void Dispose()
        {
            //TODO LOg

            _processingServer.Dispose();
            //Logger.Info("Hangfire Server stopped.");
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

            //processes.Add(new DelayedJobScheduler(_options.SchedulePollingInterval, stateChanger));
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
