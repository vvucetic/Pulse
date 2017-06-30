using Pulse.Core.Common;
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
            
            var context = new BackgroundProcessContext(
                serverId: _options.ServerName,
                cancellationToken: _cts.Token,
                storage: storage,
                properties: properties, 
                serverContext: options.ServerContext
                );
            _bootstrapTask = WrapProcess(this).CreateTask(context);
        }
        private IEnumerable<IBackgroundProcess> GetRequiredProcesses()
        {
            yield return new ServerHeartbeatProcess(_options.HeartbeatInterval);
            yield return new ServerWatchdogProcess(_options.ServerCheckInterval, _options.ServerTimeout);
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
                context.Storage.RemoveServer(context.ServerId);
            }
        }

        public override string ToString()
        {
            return GetType().Name;
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
