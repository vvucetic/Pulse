using Pulse.Core.Common;
using Pulse.Core.Log;
using Pulse.Core.Server;
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
        private static readonly object LockObject = new object();
        private static DataStorage _current;

        public static DataStorage Current
        {
            get
            {
                lock (LockObject)
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException("DataStorage.Current property value has not been initialized. You must set it before using Pulse Client or Server API.");
                    }

                    return _current;
                }
            }
            set
            {
                lock (LockObject)
                {
                    _current = value;
                }
            }
        }
        public virtual void Dispose()
        {
        }
        
        // Job operations

        public abstract QueueJob FetchNextJob(string[] queue, string workerId);
        public abstract int CreateAndEnqueueJob(QueueJob queueJob);
        public abstract void CreateAndEnqueueWorkflow(Workflow workflow);
        public abstract void SetJobState(int jobId, IState state);
        public abstract void UpgradeFailedToScheduled(int jobId, IState failedState, IState scheduledState, DateTime nextRun, int retryCount);
        public abstract void ExpireJob(int jobId);
        public abstract void DeleteJob(int jobId);

        //Queue operations

        public abstract void AddToQueue(int jobId, string queue);
        public abstract void RemoveFromQueue(int jobId);
        public abstract void Requeue(int queueJobId);
        public abstract bool EnqueueNextDelayedJob();

        //Infrastructure operations

        public abstract void HeartbeatServer(string serverId, string data);
        public abstract void RemoveServer(string serverId);
        public abstract void RegisterWorker(string workerId, string serverId);
        public abstract int RemoveTimedOutServers(TimeSpan timeout);
        public abstract IEnumerable<IBackgroundProcess> GetStorageProcesses();
        public virtual void WriteOptionsToLog(ILog logger) { }

        //Recurring task operations

        public abstract bool EnqueueNextScheduledItem(Func<ScheduledTask, ScheduledTask> caluculateNext);
        public abstract int CreateOrUpdateRecurringTask(ScheduledTask job);
        public abstract int RemoveScheduledItem(string name);
        public abstract void TriggerScheduledJob(string name);

        //Workflow operations

        public abstract void EnqueueAwaitingWorkflowJobs(int finishedJobId);
        public abstract void MarkConsequentlyFailedJobs(int failedJobId);

    }
}
