using NPoco;
using Pulse.Core.Common;
using Pulse.Core.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.Core.Storage
{
    public abstract class DataStorage
    {
        public virtual void Dispose()
        {
        }

        //public abstract void FetchNextJob(string[] queues, CancellationToken cancellationToken);

        //public abstract void GetJobData(string jobId);

        public abstract QueueJob FetchNextJob(string[] queue);

        public abstract int CreateAndEnqueue(QueueJob queueJob);
        public abstract void PersistJob(int jobId);
        public abstract void InsertAndSetJobState(int jobId, IState state);
        public abstract void InsertAndSetJobStates(int jobId, params IState[] state);
        public abstract void UpgradeStateToScheduled(int jobId, IState oldState, IState newState, DateTime nextRun, int retryCount);
        public abstract int InsertJobState(int jobId, IState state);
        public abstract void AddToQueue(int jobId, string queue);
        public abstract void RemoveFromQueue(int jobId);
        public abstract void Requeue(int queueJobId);
        public abstract void WrapTransaction(Action<Database> action);
        public abstract bool EnqueueNextDelayedJob();
    }
}
