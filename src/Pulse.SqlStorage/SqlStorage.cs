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
using Pulse.Core.Server;
using Pulse.SqlStorage.Processes;

namespace Pulse.SqlStorage
{
    public class SqlStorage : DataStorage
    {
        private readonly string _connectionStringName;
        private readonly SqlServerStorageOptions _options;
        private readonly IQueryService _queryService;
        
        public SqlStorage(string connectionStringName) : this(connectionStringName, new SqlServerStorageOptions())
        {
        }

        public SqlStorage(string connectionStringName, SqlServerStorageOptions options) : this (connectionStringName, options, new QueryService(options))
        {

        }

        public SqlStorage(string connectionStringName, SqlServerStorageOptions options, IQueryService queryService)
        {
            this._connectionStringName = connectionStringName ?? throw new ArgumentNullException(nameof(connectionStringName));
            this._options = options ?? throw new ArgumentNullException(nameof(options));
            this._queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            CustomDatabaseFactory.Setup(options.SchemaName, GetConnectionString(connectionStringName));
            Initialize();
        }

        #region Job operations
        
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
                    
                    var result = new QueueJob()
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
                        WorkflowId = jobEntity.WorkflowId,
                        ScheduleName = jobEntity.ScheduleName,
                        Description = jobEntity.Description
                    };
                    tran.Complete();
                    return result;
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
            var insertedJob = JobEntity.FromQueueJob(queueJob);
            insertedJob.CreatedAt = DateTime.UtcNow;
            insertedJob.RetryCount = 0;
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

        public override void ExpireJob(int jobId)
        {
            using (var db = GetDatabase())
            {
                db.Update<JobEntity>(new JobEntity() { ExpireAt = DateTime.UtcNow.Add(_options.DefaultJobExpiration) }, t => t.ExpireAt);
            }
        }

        public override void DeleteJob(int jobId)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var stateId = _queryService.InsertJobState(StateEntity.FromIState(new DeletedState(), jobId), db);
                    _queryService.UpdateJob(new JobEntity() { Id = jobId, StateId = stateId, State = DeletedState.DefaultName, ExpireAt = DateTime.UtcNow.Add(_options.DefaultJobExpiration) }, t => new { t.State, t.StateId, t.ExpireAt }, db);
                }
            }
        }


        public override void SetJobState(int jobId, IState state)
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
        
        public override void UpgradeFailedToScheduled(int jobId, IState failedState, IState scheduledState, DateTime nextRun, int retryCount)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    this._queryService.InsertJobState(StateEntity.FromIState(failedState, jobId), db);
                    var stateId = this._queryService.InsertJobState(StateEntity.FromIState(scheduledState, jobId), db);
                    this._queryService.UpdateJob(
                        new JobEntity { Id = jobId, NextRetry = nextRun, RetryCount = retryCount, StateId = stateId, State = scheduledState.Name }, 
                        t => new { t.RetryCount, t.NextRetry, t.State, t.StateId },
                        db);
                    tran.Complete();
                }
            }
        }

        #endregion

        #region Queue operation

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
                this._queryService.UpdateQueue(new QueueEntity() { Id = queueJobId, FetchedAt = null, WorkerId = null }, t => new { t.WorkerId, t.FetchedAt }, db);
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

        #endregion

        #region Infrastructure operations

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

        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Log("Using the following options for SQL Server job storage:");
            logger.Log($"    Queue poll interval: {_options.QueuePollInterval}.");
        }

        public override IEnumerable<IBackgroundProcess> GetStorageProcesses()
        {
            yield return new ExpirationManager(this, _options.JobExpirationCheckInterval, _options.SchemaName);
        }

        #endregion

        #region Recurring task operations

        public override bool EnqueueNextScheduledItem(Func<ScheduledTask, ScheduledTask> caluculateNext )
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
                    var result = EnqueueScheduledTaskAndRecalculateNextTime(scheduleEntity, "Enqueued by Recurring Scheduler", caluculateNext, db);
                    tran.Complete();
                    return result;
                }
            }
        }

        private bool EnqueueScheduledTaskAndRecalculateNextTime(ScheduleEntity scheduleEntity, string reasonToEnqueue, Func<ScheduledTask, ScheduledTask> caluculateNext, Database db)
        {
            var scheduledTask = ScheduleEntity.ToScheduleTask(scheduleEntity);

            //if calculate delegate exists, calculate next invocation and save, otherwise (manual trigger), skip
            if (caluculateNext != null)
            {
                scheduledTask = caluculateNext(scheduledTask);
                scheduleEntity.LastInvocation = scheduledTask.LastInvocation;
                scheduleEntity.NextInvocation = scheduledTask.NextInvocation;
                this._queryService.UpdateScheduledItem(scheduleEntity, t => new { t.LastInvocation, t.NextInvocation }, db);
            }

            //if job scheduled
            if (!string.IsNullOrEmpty(scheduleEntity.JobInvocationData))
            {
                var insertedJob = JobEntity.FromScheduleEntity(scheduledTask);
                insertedJob.ScheduleName = scheduleEntity.Name;
                var jobId = this._queryService.InsertJob(insertedJob, db);
                var stateId = this._queryService.InsertJobState(
                    StateEntity.FromIState(new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = scheduledTask.Job.QueueName, Reason = reasonToEnqueue },
                    insertedJob.Id),
                    db);
                this._queryService.SetJobState(jobId, stateId, EnqueuedState.DefaultName, db);
                this._queryService.InsertJobToQueue(jobId, scheduledTask.Job.QueueName, db);
                return true;
            }
            //if workflow scheduled
            else if (!string.IsNullOrEmpty(scheduleEntity.WorkflowInvocationData))
            {
                var rootJobs = scheduledTask.Workflow.GetRootJobs().ToDictionary(t => t.TempId, null);
                scheduledTask.Workflow.SaveWorkflow((workflowJob) => {
                    workflowJob.QueueJob.ScheduleName = scheduleEntity.Name;
                    return CreateAndEnqueueJob(
                        queueJob: workflowJob.QueueJob,
                        state: rootJobs.ContainsKey(workflowJob.TempId) ?
                            new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = workflowJob.QueueJob.QueueName, Reason = reasonToEnqueue } as IState
                            : new AwaitingState() { Reason = "Waiting for other job/s to finish.", CreatedAt = DateTime.UtcNow } as IState,
                        db: db
                        );
                });
                return true;
            }
            else
            {
                return false;
            }
        }

        public override int CreateOrUpdateRecurringTask(ScheduledTask job)
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

        public override int RemoveScheduledItem(string name)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var result = this._queryService.RemoveScheduledItem(name, db);
                    tran.Complete();
                    return result;
                }
            }
        }

        public override void TriggerScheduledJob(string name)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var scheduleEntity = this._queryService.LockScheduledItem(name, db);
                    if (scheduleEntity == null)
                    {
                        return;
                    }
                    var result = EnqueueScheduledTaskAndRecalculateNextTime(scheduleEntity, "Enqueued because scheduled job was triggered manually.", caluculateNext: null, db: db);
                    tran.Complete();
                    return;
                }
            }
        }
        #endregion

        #region Workflow operations
        
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
                                new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = workflowJob.QueueJob.QueueName, Reason = "Automatically enqueued as part of workflow because not parent jobs to wait for." } as IState
                                : new AwaitingState() { Reason = "Waiting for other job/s to finish.", CreatedAt = DateTime.UtcNow } as IState,
                            db: db
                            );
                    });

                    tran.Complete();
                }
            }
        }

        public override void EnqueueAwaitingWorkflowJobs(int finishedJobId)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var nextJobs = this._queryService.MarkAsFinishedAndGetNextJobs(finishedJobId, db);
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
        
        public override void MarkConsequentlyFailedJobs(int failedJobId)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var dependentJobs = this._queryService.GetDependentWorkflowTree(failedJobId, db);
                    var state = new ConsequentlyFailed("Job marked as failed because one of jobs this job depends on has failed.", failedJobId);
                    foreach (var job in dependentJobs)
                    {
                        var stateId = this._queryService.InsertJobState(StateEntity.FromIState(state, job.Id), db);
                        this._queryService.SetJobState(job.Id, stateId, state.Name, db);
                    }
                    tran.Complete();
                }
            }
        }

        #endregion

        #region Helpers

        private void Initialize()
        {
            if (_options.PrepareSchemaIfNecessary)
            {
                using (var db = GetDatabase())
                {
                    SqlServerObjectsInstaller.Install(db, _options.SchemaName);
                };
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

        internal Database GetDatabase(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            return CustomDatabaseFactory.DbFactory.GetDatabase();
        }
        #endregion

    }
}
