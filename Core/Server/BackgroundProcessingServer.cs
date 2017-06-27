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
                GetGloballyUniqueServerId(),
                _cts.Token,
                storage
                );
            _bootstrapTask = WrapProcess(this).CreateTask(context);
        }
        private IEnumerable<IBackgroundProcess> GetRequiredProcesses()
        {
            yield return new ServerHeartbeat(_options.HeartbeatInterval);
        }
        public void SendStop()  
        {
            _cts.Cancel();
        }

        private static IBackgroundProcess WrapProcess(IBackgroundProcess process)
        {
            return new InfiniteLoopProcess(new AutomaticRetryProcess(process));
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

        public void Execute(BackgroundProcessContext context)
        {
            //using (var connection = context.Storage.GetConnection())
            //{
            //    var serverContext = GetServerContext(context.Properties);
            //    connection.AnnounceServer(context.ServerId, serverContext);
            //}

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

        public void Dispose()
        {
            SendStop();

            // TODO: Dispose _cts

            if (!_bootstrapTask.Wait(_options.ShutdownTimeout))
            {
                //TODO Log
                //Logger.Warn("Processing server takes too long to shutdown. Performing ungraceful shutdown.");
            }
        }
    }
}
