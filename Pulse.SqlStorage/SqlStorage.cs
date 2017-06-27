using NPoco;
using Pulse.Core.Common;
using Pulse.Core.Exceptions;
using Pulse.Core.Storage;
using Pulse.SqlStorage.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public class SqlStorage : DataStorage
    {
        private readonly string _connectionStringName;

        //Database _database;

        public SqlStorage(string connectionStringName)
        {
            if (connectionStringName == null) throw new ArgumentNullException(nameof(connectionStringName));
            this._connectionStringName = connectionStringName;
        }
        public override QueueJob FetchNextJob(string[] queues)
        {
            var fetchJobSqlTemplate = $@"
set transaction isolation level read committed
update top (1) q
set FetchedAt = GETUTCDATE()
output INSERTED.Id as QueueJobId, INSERTED.JobId, INSERTED.Queue, INSERTED.FetchedAt
from JobQueue q with (readpast, updlock, rowlock, forceseek)
where Queue IN (@queues) and
(FetchedAt is null or FetchedAt < DATEADD(second, @timeout, GETUTCDATE()))";
            using (var db = GetDatabase())
            {
                var fetchedJob = db.FirstOrDefault<FetchedJob>(fetchJobSqlTemplate, new { queues = queues, timeout = TimeSpan.FromHours(2).Seconds });
                if (fetchedJob == null)
                    return null;

                var jobEntity = db.FirstOrDefault<JobEntity>("SELECT [Id],[State],[InvocationData],[Arguments],[CreatedAt],[NextJobs],[ContextId],[NumberOfConditionJobs] FROM Job WHERE Id = @0", fetchedJob.JobId);
                if (jobEntity == null)
                    throw new InternalErrorException("Job not found after fetched from queue.");

                var invocationData = JobHelper.FromJson<InvocationData>(jobEntity.InvocationData);
                var nextJobs = JobHelper.FromJson<List<int>>(jobEntity.NextJobs);
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
                    CreatedAt = jobEntity.CreatedAt
                };
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
                        NumberOfConditionJobs = queueJob.NumberOfConditionJobs
                    };
                    db.Insert<JobEntity>(insertedJob);
                    db.Insert<StateEntity>(new StateEntity() {
                        CreatedAt = DateTime.UtcNow,
                        JobId = insertedJob.Id,
                        Name = "Enqueued",
                        Reason = null,
                        Data = null
                    });
                    var insertedQueueItem = new QueueEntity()
                    {
                        JobId = insertedJob.Id,
                        Queue = queueJob.QueueName
                    };
                    db.Insert<QueueEntity>(insertedQueueItem);
                    tran.Complete();
                    return insertedJob.Id;
                }
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
    }
}
