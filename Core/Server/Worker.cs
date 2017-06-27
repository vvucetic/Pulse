using Pulse.Core.Common;
using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    public class Worker : IBackgroundProcess
    {
        private readonly string _workerId;

        private readonly string[] _queues;

        private readonly IBackgroundJobPerformer _performer;

        private readonly DataStorage _storage;

        public Worker(string[] queues, IBackgroundJobPerformer performer, DataStorage storage)
        {
            this._workerId = Guid.NewGuid().ToString();
            this._queues = queues ?? throw new ArgumentNullException(nameof(queues));
            this._performer = performer ?? throw new ArgumentNullException(nameof(performer));
            this._storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void Execute(BackgroundProcessContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var queueJob = _storage.FetchNextJob(_queues);

            try
            {
                var perfomContext = new PerformContext(context.CancellationToken, queueJob);
                var result = this._performer.Perform(perfomContext);
            }
            catch (Exception ex)
            {
                if (context.IsShutdownRequested)
                {
                    //Logger.Info(String.Format(
                    //    "Shutdown request requested while processing background job '{0}'. It will be re-queued.",
                    //    queueJob.JobId));
                }
                else
                {
                    //Logger.DebugException("An exception occurred while processing a job. It will be re-queued.", ex);
                }

                //TODO Requeue needs implementation
                Requeue(queueJob);
                throw;
            }

        }
        private static void Requeue(QueueJob fetchedJob)
        {
            try
            {
                //fetchedJob.Requeue();
            }
            catch (Exception ex)
            {
                //Logger.WarnException($"Failed to immediately re-queue the background job '{fetchedJob.JobId}'. Next invocation may be delayed, if invisibility timeout is used", ex);
            }
        }
        public override string ToString()
        {
            return $"{GetType().Name} #{_workerId.Substring(0, 8)}";
        }
    }
}
