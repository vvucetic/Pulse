using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    public class BackgroundJobServerOptions
    {
        // https://github.com/HangfireIO/Hangfire/issues/246
        private const int MaxDefaultWorkerCount = 20;

        private int _workerCount;
        private string[] _queues;

        public BackgroundJobServerOptions()
        {
            WorkerCount = Math.Min(Environment.ProcessorCount * 5, MaxDefaultWorkerCount);
            //Queues = new[] { EnqueuedState.DefaultQueue };
            //ShutdownTimeout = BackgroundProcessingServer.DefaultShutdownTimeout;
            //SchedulePollingInterval = DelayedJobScheduler.DefaultPollingDelay;
            HeartbeatInterval = ServerHeartbeat.DefaultHeartbeatInterval;
            //ServerTimeout = ServerWatchdog.DefaultServerTimeout;
            //ServerCheckInterval = ServerWatchdog.DefaultCheckInterval;
            
            Activator = null;
        }

        public string ServerName { get; set; }

        public int WorkerCount
        {
            get { return _workerCount; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "WorkerCount property value should be positive.");

                _workerCount = value;
            }
        }

        public string[] Queues
        {
            get { return _queues; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value.Length == 0) throw new ArgumentException("You should specify at least one queue to listen.", nameof(value));

                _queues = value;
            }
        }

        public TimeSpan ShutdownTimeout { get; set; }
        public TimeSpan SchedulePollingInterval { get; set; }
        public TimeSpan HeartbeatInterval { get; set; }
        public TimeSpan ServerTimeout { get; set; }
        public TimeSpan ServerCheckInterval { get; set; }
        
        public JobActivator Activator { get; set; }
    }
}
