using NPoco;
using Pulse.Core.Common;
using Pulse.Core.Exceptions;
using Pulse.Core.States;
using Pulse.Core.Storage;
using Pulse.SqlStorage.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulse.Core.Log;

namespace Pulse.SqlStorage
{
    public class SqlStorage : DataStorage
    {
        private readonly string _connectionStringName;
        private readonly SqlServerStorageOptions _options;
        private readonly QueryService _queryService;
        
        public SqlStorage(string connectionStringName) : this(connectionStringName, new SqlServerStorageOptions())
        {
        }

        public SqlStorage(string connectionStringName, SqlServerStorageOptions options)
        {
            this._connectionStringName = connectionStringName ?? throw new ArgumentNullException(nameof(connectionStringName));
            this._options = options ?? throw new ArgumentNullException(nameof(options));
            this._queryService = new QueryService(this._options);
        }

        public override QueueJob FetchNextJob(string[] queues, string workerId)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var fetchedJob = this._queryService.FetchNextJob(queues, workerId, db);
                    if (fetchedJob == null)
                        return null;

                    var jobEntity = this._queryService.GetJob(fetchedJob.JobId, db);
                    if (jobEntity == null)
                        throw new InternalErrorException("Job not found after fetched from queue.");

                    var invocationData = JobHelper.FromJson<InvocationData>(jobEntity.InvocationData);
                    var nextJobs = JobHelper.FromJson<List<int>>(jobEntity.NextJobs);
                    tran.Complete();
                    return new QueueJob()
                    {
                        JobId = jobEntity.Id,
                        QueueJobId = fetchedJob.QueueJobId,
                        QueueName = fetchedJob.Queue,
                        Job = invocationData.Deserialize(),
                        ContextId = jobEntity.ContextId,
                        NextJobs = nextJobs,
                        FetchedAt = fetchedJob.FetchedAt,
                        CreatedAt = jobEntity.CreatedAt,
                        ExpireAt = jobEntity.ExpireAt,
                        MaxRetries = jobEntity.MaxRetries,
                        NextRetry = jobEntity.NextRetry,
                        RetryCount = jobEntity.RetryCount,
                        WorkerId = fetchedJob.WorkerId,
                        WorkflowId = jobEntity.WorkflowId
                    };
                }
            }
        }

        public override int CreateAndEnqueueJob(QueueJob queueJob)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var state = new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = queueJob.QueueName, Reason = "Job enqueued" };
                    var jobId = CreateAndEnqueueJob(queueJob, state, db);                    
                    tran.Complete();
                    return jobId;
                }
            }
        }

        private int CreateAndEnqueueJob(QueueJob queueJob, IState state, Database db)
        {
            var insertedJob = new JobEntity()
            {
                ContextId = queueJob.ContextId,
                CreatedAt = DateTime.UtcNow,
                InvocationData = JobHelper.ToJson(queueJob.Job),
                NextJobs = JobHelper.ToJson(queueJob.NextJobs),
                RetryCount = 1,
                MaxRetries = queueJob.MaxRetries,
                NextRetry = queueJob.NextRetry,
                ExpireAt = queueJob.ExpireAt,
                Queue = queueJob.QueueName,
                WorkflowId = queueJob.WorkflowId
            };
            var jobId = this._queryService.InsertJob(insertedJob, db);
            var stateId = this._queryService.InsertJobState(
                StateEntity.FromIState(state,
                insertedJob.Id),
                db);
            this._queryService.SetJobState(jobId, stateId, state.Name, db);
            if (state is EnqueuedState)
            {
                this._queryService.InsertJobToQueue(jobId, queueJob.QueueName, db);
            }
            foreach (var nextJob in queueJob.NextJobs)
            {
                this._queryService.InsertJobCondition(new JobConditionEntity()
                {
                    Finished = false,
                    JobId = nextJob,
                    ParentJobId = jobId
                }, db);
            }
            return jobId;
        }

        public override void CreateAndEnqueueWorkflow(Workflow workflow)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var rootJobs = workflow.GetRootJobs().ToDictionary(t => t.TempId, null);
                    workflow.SaveWorkflow((workflowJob) => {
                        return CreateAndEnqueueJob(
                            queueJob: workflowJob.QueueJob,
                            state: rootJobs.ContainsKey(workflowJob.TempId) ? 
                                new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = workflowJob.QueueJob.QueueName, Reason = "Automatically enqueued as part of workflow because not parent jobs to wait for."  } as IState
                                : new AwaitingState() { Reason = "Waiting for other job/s to finish.",  CreatedAt = DateTime.UtcNow } as IState,
                            db: db
                            );
                    });
                    
                    tran.Complete();
                }
            }
        }

        //public override void PersistJob(int jobId)
        //{
        //    using (var db = GetDatabase())
        //    {
        //        db.Update<JobEntity>(new JobEntity() { ExpireAt = null }, t => t.ExpireAt);
        //    }
        //}

        public override void InsertAndSetJobState(int jobId, IState state)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var stateId = this._queryService.InsertJobState(StateEntity.FromIState(state, jobId), db);
                    this._queryService.SetJobState(jobId, stateId, state.Name, db);
                    tran.Complete();
                }
            }
        }
        
        public override void InsertAndSetJobStates(int jobId, params IState[] states)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {                    
                    for (int i = 0; i < states.Length; i++)
                    {
                        var newState = StateEntity.FromIState(states[i], jobId);

                        if (i == states.Length - 1)
                        {
                            //insert and set last state
                            this._queryService.InsertJobState(newState, db);
                            this._queryService.SetJobState(jobId, newState.Id, newState.Name, db);

                        }
                        {
                            //insert state
                            this._queryService.InsertJobState(newState, db);                            
                        }
                    }
                    tran.Complete();
                }
            }
        }

        public override void UpgradeStateToScheduled(int jobId, IState oldState, IState scheduledState, DateTime nextRun, int retryCount)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    this._queryService.InsertJobState(StateEntity.FromIState(oldState, jobId), db);
                    var stateId = this._queryService.InsertJobState(StateEntity.FromIState(scheduledState, jobId), db);
                    this._queryService.UpdateJob(
                        new JobEntity { Id = jobId, NextRetry = nextRun, RetryCount = retryCount, StateId = stateId, State = scheduledState.Name }, 
                        t => new { t.RetryCount, t.NextRetry, t.State, t.StateId },
                        db);
                    tran.Complete();
                }
            }
        }        

        public override void AddToQueue(int jobId, string queue)
        {
            using (var db = GetDatabase())
            {
                this._queryService.InsertJobToQueue(jobId, queue, db);
            }
        }

        public override void RemoveFromQueue(int queueJobId)
        {
            using (var db = GetDatabase())
            {
                this._queryService.RemoveFromQueue(queueJobId, db);
            }
        }

        public override void Requeue(int queueJobId)
        {
            using (var db = GetDatabase())
            {
                this._queryService.UpdateQueue(new QueueEntity() { Id = queueJobId, FetchedAt = null, WorkerId = null }, t=>new { t.WorkerId, t.FetchedAt }, db);
            }
        }

        public override bool EnqueueNextDelayedJob()
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction())
                {
                    var job = this._queryService.EnqueueNextDelayedJob(db);
                    if (job != null)
                    {
                        var stateId = this._queryService.InsertJobState(StateEntity.FromIState(new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = job.Queue, Reason = "Enqueued by DelayedJobScheduler" }, job.JobId), db);
                        this._queryService.SetJobState(job.JobId, stateId, EnqueuedState.DefaultName, db);
                        tran.Complete();
                        return true;
                    }
                    else
                    {
                        return false; 
                    }
                }
            }
        }

        public override void HeartbeatServer(string server, string data)
        {
            using (var db = GetDatabase())
            {
                this._queryService.HeartbeatServer(server, data, db);
            }
        }
        public override void RemoveServer(string serverId)
        {
            using (var db = GetDatabase())
            {
                this._queryService.RemoveServer(serverId, db);
            }
        }

        public override void RegisterWorker(string workerId, string serverId)
        {
            using (var db = GetDatabase())
            {
                this._queryService.RegisterWorker(workerId, serverId, db);
            }
        }

        public override int RemoveTimedOutServers(TimeSpan timeout)
        {
            if (timeout.Duration() != timeout)
            {
                throw new ArgumentException("The `timeout` value must be positive.", nameof(timeout));
            }
            using (var db = GetDatabase())
            {
                return this._queryService.RemoveTimedOutServers(timeout, db);
            }
        }

        public override bool EnqueueNextScheduledItem(Func<ScheduledJob, ScheduledJob> caluculateNext )
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var scheduleEntity = this._queryService.LockFirstScheduledItem(db);
                    if (scheduleEntity == null)
                    {
                        return false;
                    }
                    
                    if(!string.IsNullOrEmpty(scheduleEntity.JobInvocationData))
                    {
                        var scheduledJob = ScheduleEntity.ToScheduleJob(scheduleEntity);

                        scheduledJob = caluculateNext(scheduledJob);
                        scheduleEntity.LastInvocation = scheduledJob.LastInvocation;
                        scheduleEntity.NextInvocation = scheduledJob.NextInvocation;
                        this._queryService.UpdateScheduledItem(scheduleEntity, t => new { t.LastInvocation, t.NextInvocation }, db);
                        var insertedJob = JobEntity.FromScheduleEntity(scheduledJob);                        
                        var jobId = this._queryService.InsertJob(insertedJob, db);
                        var stateId = this._queryService.InsertJobState(
                            StateEntity.FromIState(new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = scheduledJob.QueueJob.QueueName, Reason = "Job enqueued by Recurring Scheduler" },
                            insertedJob.Id),
                            db);
                        this._queryService.SetJobState(jobId, stateId, EnqueuedState.DefaultName, db);
                        this._queryService.InsertJobToQueue(jobId, scheduledJob.QueueJob.QueueName, db);
                        tran.Complete();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }


        public override int CreateOrUpdateRecurringJob(ScheduledJob job)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var result = this._queryService.CreateOrUpdateRecurringJob(job, db);
                    tran.Complete();
                    return result;
                }
            }
        }

        public override void FinishJobAndEnqueueNext(int jobId)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var nextJobs = this._queryService.MarkAsFinishedAndGetNextJobs(jobId, db);
                    foreach (var job in nextJobs)
                    {
                        var stateId = this._queryService.InsertJobState(
                                                    StateEntity.FromIState(new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = job.Queue, Reason = "Job enqueued after awaited job successfully finished." },
                                                    job.Id),
                                                    db);
                        this._queryService.SetJobState(job.Id, stateId, EnqueuedState.DefaultName, db);
                        this._queryService.InsertJobToQueue(job.Id, job.Queue, db);
                    }
                    tran.Complete();
                }
            }
        }

        private string GetConnectionString(string nameOrConnectionString)
        {
            if (IsConnectionString(nameOrConnectionString))
            {
                return nameOrConnectionString;
            }

            if (IsConnectionStringInConfiguration(nameOrConnectionString))
            {
                return ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString;
            }

            throw new ArgumentException($"Could not find connection string with name '{nameOrConnectionString}' in application config file");
        }

        private bool IsConnectionString(string nameOrConnectionString)
        {
            return nameOrConnectionString.Contains(";");
        }

        private bool IsConnectionStringInConfiguration(string connectionStringName)
        {
            var connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSetting != null;
        }

        public override void Dispose()
        {
            //if (this._database != null)
            //{
            //    this._database.Dispose();
            //}
        }

        private Database GetDatabase(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            return new Database(this._connectionStringName);
        }
        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Log("Using the following options for SQL Server job storage:");
            logger.Log($"    Queue poll interval: {_options.QueuePollInterval}.");
        }
    }
}
