using Pulse.Core.Log;
using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server.Processes
{
    public class DelayedJobSchedulerProcess : IBackgroundProcess
    {
        public static readonly TimeSpan DefaultPollingDelay = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _pollingDelay;
        private readonly DataStorage _storage;
        private readonly ILog _logger = LogProvider.GetLogger();


        public DelayedJobSchedulerProcess(DataStorage storage) : this(DefaultPollingDelay, storage)
        {
        }

        public DelayedJobSchedulerProcess(TimeSpan pollingDelay, DataStorage storage)
        {
            this._storage = storage;
            this._pollingDelay = pollingDelay;
        }

        public void Execute(BackgroundProcessContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var jobsEnqueued = 0;

            while (EnqueueNextScheduledJob(context))
            {
                jobsEnqueued++;

                if (context.IsShutdownRequested)
                {
                    break;
                }
            }

            if (jobsEnqueued != 0)
            {
                _logger.Log($"{jobsEnqueued} scheduled job(s) enqueued.");
            }

            context.Wait(_pollingDelay);
        }
        public override string ToString()
        {
            return GetType().Name;
        }

        private bool EnqueueNextScheduledJob(BackgroundProcessContext context)
        {
            return this._storage.EnqueueNextDelayedJob();
        }
    }
}
