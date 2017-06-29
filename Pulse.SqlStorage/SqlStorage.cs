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
        
        public SqlStorage(string connectionStringName) : this(connectionStringName, new SqlServerStorageOptions())
        {
        }

        public SqlStorage(string connectionStringName, SqlServerStorageOptions options)
        {
            this._connectionStringName = connectionStringName ?? throw new ArgumentNullException(nameof(connectionStringName));
            this._options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public override QueueJob FetchNextJob(string[] queues)
        {
            var fetchJobSqlTemplate = $@";
update top (1) q
set q.FetchedAt = GETUTCDATE()
output INSERTED.Id as QueueJobId, INSERTED.JobId, INSERTED.Queue, INSERTED.FetchedAt
from Queue q
where q.Queue IN (@queues) and
(q.FetchedAt is null )--or q.FetchedAt < DATEADD(second, @timeout, GETUTCDATE()))";
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var fetchedJob = db.Query<FetchedJob>(fetchJobSqlTemplate, new { queues = queues, timeout = TimeSpan.FromHours(2).Seconds }).FirstOrDefault();
                    if (fetchedJob == null)
                        return null;

                    var jobEntity = db.FirstOrDefault<JobEntity>("WHERE Id = @0", fetchedJob.JobId);
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
                        NumberOfConditionJobs = jobEntity.NumberOfConditionJobs,
                        FetchedAt = fetchedJob.FetchedAt,
                        CreatedAt = jobEntity.CreatedAt,
                        ExpireAt = jobEntity.ExpireAt,
                        MaxRetries = jobEntity.MaxRetries,
                        NextRetry = jobEntity.NextRetry,
                        RetryCount = jobEntity.RetryCount
                    };
                }
            }
        }

        public override int CreateAndEnqueue(QueueJob queueJob)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    var insertedJob = new JobEntity()
                    {
                        ContextId = queueJob.ContextId,
                        CreatedAt = DateTime.UtcNow,
                        InvocationData = JobHelper.ToJson(InvocationData.Serialize(queueJob.Job)),
                        NextJobs = JobHelper.ToJson(queueJob.NextJobs),
                        NumberOfConditionJobs = queueJob.NumberOfConditionJobs,
                        RetryCount = 1,
                        MaxRetries = queueJob.MaxRetries,
                        NextRetry = queueJob.NextRetry,
                        ExpireAt = queueJob.ExpireAt,
                        Queue = queueJob.QueueName
                    };
                    db.Insert<JobEntity>(insertedJob);
                    InsertAndSetJobState(insertedJob.Id, new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = queueJob.QueueName, Reason="Job enqueued" }, db);
                    AddToQueue(insertedJob.Id, queueJob.QueueName, db);
                    tran.Complete();
                    return insertedJob.Id;
                }
            }
        }

        public override void PersistJob(int jobId)
        {
            using (var db = GetDatabase())
            {
                db.Update<JobEntity>(new JobEntity() { ExpireAt = null }, t => t.ExpireAt);
            }
        }

        public override void InsertAndSetJobState(int jobId, IState state)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    InsertAndSetJobState(jobId, state, db);
                    tran.Complete();
                }
            }
        }

        public void InsertAndSetJobState(int jobId, IState state, Database db)
        {
            var newState = StateEntity.FromIState(state, jobId);
            db.Insert<StateEntity>(newState);
            db.Update<JobEntity>(new JobEntity() { Id = jobId, State = newState.Name, StateId = newState.Id }, t => new { t.State, t.StateId });                      
        }

        public override void InsertAndSetJobStates(int jobId, params IState[] states)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {                    
                    for (int i = 0; i < states.Length; i++)
                    {
                        if(i == states.Length - 1)
                        {
                            //insert and set last state
                            InsertAndSetJobState(jobId, states[i], db);
                        }
                        {
                            //insert state
                            InsertJobState(jobId, states[i], db);
                        }
                    }
                    tran.Complete();
                }
            }
        }

        public override void UpgradeStateToScheduled(int jobId, IState oldState, IState newState, DateTime nextRun, int retryCount)
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    InsertJobState(jobId, oldState, db);
                    var stateId = InsertJobState(jobId, newState, db);
                    db.Update<JobEntity>(new JobEntity { Id = jobId, NextRetry = nextRun, RetryCount = retryCount, StateId = stateId, State = newState.Name }, t => new { t.RetryCount, t.NextRetry, t.State, t.StateId });
                    tran.Complete();
                }
            }
        }

        public override int InsertJobState(int jobId, IState state)
        {
            using (var db = GetDatabase())
            {
                var newState = StateEntity.FromIState(state, jobId);
                db.Insert<StateEntity>(newState);
                return newState.Id;
            }
        }

        public int InsertJobState(int jobId, IState state, Database db)
        {
            var newState = StateEntity.FromIState(state, jobId);
            db.Insert<StateEntity>(newState);
            return newState.Id;          
        }

        public override void AddToQueue(int jobId, string queue)
        {
            using (var db = GetDatabase())
            {
                AddToQueue(jobId, queue, db);
            }
        }
        public void AddToQueue(int jobId, string queue, Database db)
        {
            var insertedQueueItem = new QueueEntity()
            {
                JobId = jobId,
                Queue = queue
            };
            db.Insert<QueueEntity>(insertedQueueItem);
        }

        public override void RemoveFromQueue(int queueJobId)
        {
            using (var db = GetDatabase())
            {
                db.Delete<QueueEntity>(queueJobId);
            }
        }

        public override void Requeue(int queueJobId)
        {
            using (var db = GetDatabase())
            {
                db.Update<QueueEntity>(new QueueEntity() { Id = queueJobId, FetchedAt = null }, t=>t.FetchedAt );
            }
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

        public override void WrapTransaction(Action<Database> action)
        {
            using (var db = GetDatabase())
            {
                using (db.GetTransaction(IsolationLevel.ReadCommitted))
                {
                    action.Invoke(db);
                }
            }
        }

        public override bool EnqueueNextDelayedJob()
        {
            using (var db = GetDatabase())
            {
                using (var tran = db.GetTransaction())
                {
                    var sql = @";
DECLARE @@Ids table(Id int, [Queue] [nvarchar](50))

update top (1) j
set j.NextRetry = NULL
output INSERTED.Id as Id, inserted.[Queue] INTO @@Ids
from Job j
WHERE j.NextRetry IS NOT NULL AND j.NextRetry < GETUTCDATE()

INSERT [Queue] (JobId, [Queue])
OUTPUT INSERTED.JobId, INSERTED.[Queue], INSERTED.[FetchedAt]
SELECT [Id], [Queue] FROM @@Ids;";
                    var job = db.Query<FetchedJob>(sql).FirstOrDefault();
                    if (job != null)
                    {
                        InsertAndSetJobState(job.JobId, new EnqueuedState() { EnqueuedAt = DateTime.UtcNow, Queue = job.Queue, Reason = "Enqueued by DelayedJobScheduler" }, db);
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
            var sql =
$@"; merge Server as Target
using (VALUES(@server, @data, @heartbeat)) as Source (Id, Data, Heartbeat)
on Target.Id = Source.Id
when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat
when not matched then insert(Id, Data, LastHeartbeat) values(Source.Id, Source.Data, Source.Heartbeat);
            ";
            using (var db = GetDatabase())
            {
                db.Execute(sql, new { server = server, data = data, heartbeat = DateTime.UtcNow });
            }
        }
       

        public override void HeartbeatWorker(string worker, string server, string data)
        {
            var sql =
$@"; merge Worker as Target
using (VALUES(@worker, @server, @data, @heartbeat)) as Source (Id, Server, Data, Heartbeat)
on Target.Id = Source.Id
when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat, Server = Source.Server
when not matched then insert(Id, Server, Data, LastHeartbeat) values(Source.Id, Source.Server, Source.Data, Source.Heartbeat);
            ";
            using (var db = GetDatabase())
            {
                db.Execute(sql, new { worker = worker, server = server, data = data, heartbeat = DateTime.UtcNow });
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

        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Log("Using the following options for SQL Server job storage:");
            logger.Log($"    Queue poll interval: {_options.QueuePollInterval}.");
        }
    }
}
