using Pulse.Core.Common;
using Pulse.Core.Exceptions;
using Pulse.Core.States;
using Pulse.Core.Storage;
using Pulse.SqlStorage.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulse.Core.Log;
using Pulse.Core.Server;
using Pulse.SqlStorage.Processes;
using Pulse.Core.Monitoring;
using System.Data.Common;
using System.Data.SqlClient;
using System.Transactions;
using Dapper.Contrib.Extensions;

namespace Pulse.SqlStorage
{
    public class SqlStorage : DataStorage
    {
        private readonly string _connectionStringName;
        internal readonly SqlServerStorageOptions _options;
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
            Initialize();
        }

        internal int? CommandTimeout => _options.CommandTimeout.HasValue ? (int)_options.CommandTimeout.Value.TotalSeconds : (int?)null;

        #region Job operations

        public override QueueJob FetchNextJob(string[] queues, string workerId)
        {
            return UseTransaction<QueueJob>((conn, tran) =>
            {
                var fetchedJob = this._queryService.FetchNextJob(queues, workerId, conn, tran);
                if (fetchedJob == null)
                    return null;

                var jobEntity = this._queryService.GetJob(fetchedJob.JobId, conn);
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
                    Description = jobEntity.Description,
                    State = jobEntity.State,
                    StateId = jobEntity.StateId
                };
                return result;
            });
            
        }

        public override int CreateAndEnqueueJob(QueueJob queueJob)
        {
            return UseTransaction<int>((conn, tran) =>
            {
                var state = new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = queueJob.QueueName, Reason = "Job enqueued" };
                var jobId = CreateAndEnqueueJob(queueJob, state, conn);
                return jobId;
            });
            
        }

        private int CreateAndEnqueueJob(QueueJob queueJob, IState state, DbConnection connection)
        {
            var insertedJob = JobEntity.FromQueueJob(queueJob);
            insertedJob.CreatedAt = DateTime.UtcNow;
            insertedJob.RetryCount = 1;
            var jobId = this._queryService.InsertJob(insertedJob, connection);
            var stateId = this._queryService.InsertJobState(
                StateEntity.FromIState(state,
                insertedJob.Id),
                connection);
            this._queryService.SetJobState(jobId, stateId, state.Name, connection);
            if (state is EnqueuedState)
            {
                this._queryService.InsertJobToQueue(jobId, queueJob.QueueName, connection);
            }
            foreach (var nextJob in queueJob.NextJobs)
            {
                this._queryService.InsertJobCondition(new JobConditionEntity()
                {
                    Finished = false,
                    JobId = nextJob,
                    ParentJobId = jobId
                }, connection);
            }
            return jobId;
        }

        public override void ExpireJob(int jobId)
        {
            UseConnection(connection =>
            {
                connection.Update<JobEntity>(new JobEntity() { ExpireAt = DateTime.UtcNow.Add(_options.DefaultJobExpiration) }, t => new { t.ExpireAt });
            });
        }

        public override void DeleteJob(int jobId)
        {
            UseTransaction((conn, tran) =>
            {
                var stateId = _queryService.InsertJobState(StateEntity.FromIState(new DeletedState(), jobId), conn);
                _queryService.UpdateJob(new JobEntity() { Id = jobId, StateId = stateId, State = DeletedState.DefaultName, ExpireAt = DateTime.UtcNow.Add(_options.DefaultJobExpiration) }, t => new { t.State, t.StateId, t.ExpireAt }, conn);
            });            
        }


        public override void SetJobState(int jobId, IState state)
        {
            UseTransaction((conn, tran) =>
            {
                var stateId = this._queryService.InsertJobState(StateEntity.FromIState(state, jobId), conn);
                this._queryService.SetJobState(jobId, stateId, state.Name, conn);
            });            
        }
        
        public override void UpgradeFailedToScheduled(int jobId, IState failedState, IState scheduledState, DateTime nextRun, int retryCount)
        {
            UseTransaction((conn, tran) =>
            {
                this._queryService.InsertJobState(StateEntity.FromIState(failedState, jobId), conn);
                var stateId = this._queryService.InsertJobState(StateEntity.FromIState(scheduledState, jobId), conn);
                this._queryService.UpdateJob(
                    new JobEntity { Id = jobId, NextRetry = nextRun, RetryCount = retryCount, StateId = stateId, State = scheduledState.Name },
                    t => new { t.RetryCount, t.NextRetry, t.State, t.StateId },
                    conn);
            });
        }

        #endregion

        #region Queue operation

        public override void AddToQueue(int jobId, string queue)
        {
            UseConnection(connection =>
            {
                this._queryService.InsertJobToQueue(jobId, queue, connection);
            });
        }

        public override void RemoveFromQueue(int queueJobId)
        {
            UseConnection(connection =>
            {
                this._queryService.RemoveFromQueue(queueJobId, connection);
            });
        }

        public override void Requeue(int queueJobId)
        {
            UseConnection(connection =>
            {
                this._queryService.UpdateQueue(new QueueEntity() { Id = queueJobId, FetchedAt = null, WorkerId = null }, t => new { t.WorkerId, t.FetchedAt }, connection);
            });
        }

        public override bool EnqueueNextDelayedJob()
        {
            return UseTransaction<bool>((conn, tran) =>
            {
                var job = this._queryService.EnqueueNextDelayedJob(conn);
                if (job != null)
                {
                    var stateId = this._queryService.InsertJobState(StateEntity.FromIState(new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = job.Queue, Reason = "Enqueued by DelayedJobScheduler" }, job.JobId), conn);
                    this._queryService.SetJobState(job.JobId, stateId, EnqueuedState.DefaultName, conn);
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        #endregion

        #region Infrastructure operations

        public override void HeartbeatServer(string server, string data)
        {
            UseConnection(connection =>
            {
                this._queryService.HeartbeatServer(server, data, connection);
            });
        }

        public override void RemoveServer(string serverId)
        {
            UseConnection(connection =>
            {
                this._queryService.RemoveServer(serverId, connection);
            });
        }

        public override void RegisterWorker(string workerId, string serverId)
        {
            UseConnection(connection =>
            {
                this._queryService.RegisterWorker(workerId, serverId, connection);
            });
        }

        public override int RemoveTimedOutServers(TimeSpan timeout)
        {
            if (timeout.Duration() != timeout)
            {
                throw new ArgumentException("The `timeout` value must be positive.", nameof(timeout));
            }
            return UseConnection<int>(connection =>
            {
                return this._queryService.RemoveTimedOutServers(timeout, connection);
            });
                
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
            return UseTransaction<bool>((conn, tran) =>
            {
                var scheduleEntity = this._queryService.LockFirstScheduledItem(conn);
                if (scheduleEntity == null)
                {
                    return false;
                }
                var result = EnqueueScheduledTaskAndRecalculateNextTime(scheduleEntity, "Enqueued by Recurring Scheduler", caluculateNext, conn);
                return result;
            });
        }

        private bool EnqueueScheduledTaskAndRecalculateNextTime(ScheduleEntity scheduleEntity, string reasonToEnqueue, Func<ScheduledTask, ScheduledTask> caluculateNext, DbConnection connection)
        {
            var scheduledTask = ScheduleEntity.ToScheduleTask(scheduleEntity);

            //if calculate delegate exists, calculate next invocation and save, otherwise (manual trigger), skip
            if (caluculateNext != null)
            {
                scheduledTask = caluculateNext(scheduledTask);
                scheduleEntity.LastInvocation = scheduledTask.LastInvocation;
                scheduleEntity.NextInvocation = scheduledTask.NextInvocation;
                this._queryService.UpdateScheduledItem(scheduleEntity, t => new { t.LastInvocation, t.NextInvocation }, connection);
            }

            //if job scheduled
            if (!string.IsNullOrEmpty(scheduleEntity.JobInvocationData))
            {
                var insertedJob = JobEntity.FromScheduleEntity(scheduledTask);
                insertedJob.ScheduleName = scheduleEntity.Name;
                var jobId = this._queryService.InsertJob(insertedJob, connection);
                var stateId = this._queryService.InsertJobState(
                    StateEntity.FromIState(new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = scheduledTask.Job.QueueName, Reason = reasonToEnqueue },
                    insertedJob.Id),
                    connection);
                this._queryService.SetJobState(jobId, stateId, EnqueuedState.DefaultName, connection);
                this._queryService.InsertJobToQueue(jobId, scheduledTask.Job.QueueName, connection);
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
                        connection: connection
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
            return UseTransaction<int>((conn, tran) =>
            {
                var result = this._queryService.CreateOrUpdateRecurringJob(job, conn);
                return result;
            });            
        }

        public override bool RemoveScheduledItem(string name)
        {
            return UseTransaction((conn, tran) =>
            {
                var result = this._queryService.RemoveScheduledItem(name, conn);
                return result;
            });
        }    

        public override void TriggerScheduledJob(string name)
        {
            UseTransaction((conn, tran) =>
            {
                var scheduleEntity = this._queryService.LockScheduledItem(name, conn);
                if (scheduleEntity == null)
                {
                    return;
                }
                var result = EnqueueScheduledTaskAndRecalculateNextTime(scheduleEntity, "Enqueued because scheduled job was triggered manually.", caluculateNext: null, connection: conn);
            });
        }
        #endregion

        #region Workflow operations
        
        public override void CreateAndEnqueueWorkflow(Workflow workflow)
        {
            UseTransaction((conn, tran) =>
            {
                var rootJobs = workflow.GetRootJobs().ToDictionary(t => t.TempId, null);
                workflow.SaveWorkflow((workflowJob) => {
                    return CreateAndEnqueueJob(
                        queueJob: workflowJob.QueueJob,
                        state: rootJobs.ContainsKey(workflowJob.TempId) ?
                            new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = workflowJob.QueueJob.QueueName, Reason = "Automatically enqueued as part of workflow because not parent jobs to wait for." } as IState
                            : new AwaitingState() { Reason = "Waiting for other job/s to finish.", CreatedAt = DateTime.UtcNow } as IState,
                        connection: conn
                        );
                });
            });
        }

        public override void EnqueueAwaitingWorkflowJobs(int finishedJobId)
        {
            UseTransaction((conn, tran) =>
            {
                var nextJobs = this._queryService.MarkAsFinishedAndGetNextJobs(finishedJobId, conn);
                foreach (var job in nextJobs)
                {
                    var stateId = this._queryService.InsertJobState(
                                                StateEntity.FromIState(new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = job.Queue, Reason = "Job enqueued after awaited job successfully finished." },
                                                job.Id),
                                                conn);
                    this._queryService.SetJobState(job.Id, stateId, EnqueuedState.DefaultName, conn);
                    this._queryService.InsertJobToQueue(job.Id, job.Queue, conn);
                }
            });
        }
        
        public override void MarkConsequentlyFailedJobs(int failedJobId)
        {
            UseTransaction((conn, tran) =>
            {
                var dependentJobs = this._queryService.GetDependentWorkflowTree(failedJobId, conn);
                var state = new ConsequentlyFailed("Job marked as failed because one of jobs this job depends on has failed.", failedJobId);
                foreach (var job in dependentJobs)
                {
                    var stateId = this._queryService.InsertJobState(StateEntity.FromIState(state, job.Id), conn);
                    this._queryService.SetJobState(job.Id, stateId, state.Name, conn);
                }
            });
        }

        #endregion

        public override IMonitoringApi GetMonitoringApi()
        {
            return new SqlStorageMonitoringApi(this);
        }

        #region Helpers

        internal void UseConnection(Action<DbConnection> action)
        {
            UseConnection(connection =>
            {
                action(connection);
                return true;
            });
        }

        internal T UseConnection<T>(Func<DbConnection, T> func)
        {
            DbConnection connection = null;

            try
            {
                connection = CreateAndOpenConnection();
                return func(connection);
            }
            finally
            {
                ReleaseConnection(connection);
            }
        }

        internal void UseTransaction(Action<DbConnection, DbTransaction> action)
        {
            UseTransaction((connection, transaction) =>
            {
                action(connection, transaction);
                return true;
            }, null);
        }

        internal T UseTransaction<T>(Func<DbConnection, DbTransaction, T> func, IsolationLevel? isolationLevel = null)
        {
            using (var transaction = CreateTransaction(isolationLevel ?? IsolationLevel.ReadCommitted))
            {
                var result = UseConnection(connection =>
                {
                    connection.EnlistTransaction(Transaction.Current);
                    return func(connection, null);
                });

                transaction.Complete();

                return result;
            }
        }

        private TransactionScope CreateTransaction(IsolationLevel? isolationLevel)
        {
            return isolationLevel != null
                ? new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = isolationLevel.Value, Timeout = _options.TransactionTimeout })
                : new TransactionScope();
        }

        private void Initialize()
        {
            if (_options.PrepareSchemaIfNecessary)
            {
                UseConnection(connection =>
                {
                    SqlServerObjectsInstaller.Install(connection, _options.SchemaName);
                });                              
            }
        }

        internal DbConnection CreateAndOpenConnection()
        {
            var connection = new SqlConnection(GetConnectionString(_connectionStringName));
            connection.Open();

            return connection;
        }

        internal void ReleaseConnection(System.Data.IDbConnection connection)
        {
            if (connection != null)
            {
                connection.Dispose();
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
        }
        
        #endregion

    }
}
