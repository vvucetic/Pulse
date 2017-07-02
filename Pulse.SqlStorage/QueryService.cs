﻿using NPoco;
using Pulse.Core.Common;
using Pulse.SqlStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    internal class QueryService
    {
        private readonly SqlServerStorageOptions _options;
        public QueryService(SqlServerStorageOptions options)
        {
           this._options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public FetchedJob FetchNextJob(string[] queues, string workerId, Database db)
        {
            var fetchJobSqlTemplate = $@";
update top (1) q
set q.FetchedAt = GETUTCDATE(), q.WorkerId = @workerId
output INSERTED.Id as QueueJobId, INSERTED.JobId, INSERTED.Queue, INSERTED.FetchedAt
from Queue q
where q.Queue IN (@queues) and q.WorkerId is null";
            var fetchedJob = db.Query<FetchedJob>(fetchJobSqlTemplate, new { queues = queues, workerId = workerId }).FirstOrDefault();
            return fetchedJob;
        }

        public FetchedJob EnqueueNextDelayedJob(Database db)
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
            return db.Query<FetchedJob>(sql).FirstOrDefault();
        }

        public JobEntity GetJob(int jobId, Database db)
        {
            return db.FirstOrDefault<JobEntity>("WHERE Id = @0", jobId);
        }

        public int InsertJob(JobEntity job, Database db)
        {
            db.Insert<JobEntity>(job);
            return job.Id;
        }

        public int UpdateJob(JobEntity job, Expression<Func<JobEntity, object>> fields, Database db)
        {
            return db.Update<JobEntity>(job, fields);
        }

        public int InsertJobState(StateEntity state, Database db)
        {
            db.Insert<StateEntity>(state);
            return state.Id;
        }

        public int SetJobState(int jobId, int stateId, string stateName, Database db)
        {
            return db.Update<JobEntity>(new JobEntity() { Id = jobId, State = stateName, StateId = stateId }, t => new { t.State, t.StateId });
        }      

        public int InsertJobToQueue(int jobId, string queue, Database db)
        {
            var insertedQueueItem = new QueueEntity()
            {
                JobId = jobId,
                Queue = queue
            };
            db.Insert<QueueEntity>(insertedQueueItem);
            return insertedQueueItem.Id;
        }

        public int RemoveFromQueue(int queueJobId, Database db)
        {
            return db.Delete<QueueEntity>(queueJobId);
        }

        public int UpdateQueue(QueueEntity queueEntity, Expression<Func<QueueEntity, object>> fields, Database db)
        {
            return db.Update<QueueEntity>(queueEntity, fields);
        }

        public int RemoveTimedOutServers(TimeSpan timeout, Database db)
        {
            var sql = $@"delete from Server where LastHeartbeat < @timeoutAt";
            return db.Execute(sql, new { timeoutAt = DateTime.UtcNow.Add(timeout.Negate()) });
        }

        public int HeartbeatServer(string server, string data, Database db)
        {
            var sql =
$@"; merge Server as Target
using (VALUES(@server, @data, @heartbeat)) as Source (Id, Data, Heartbeat)
on Target.Id = Source.Id
when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat
when not matched then insert(Id, Data, LastHeartbeat) values(Source.Id, Source.Data, Source.Heartbeat);
            ";
            return db.Execute(sql, new { server = server, data = data, heartbeat = DateTime.UtcNow });
            
        }
        public int RemoveServer(string serverId, Database db)
        {
            var sql = $@"; DELETE FROM SERVER WHERE Id = @serverId";            
             return db.Execute(sql, new { serverId = serverId });            
        }

        public int RegisterWorker(string workerId, string serverId, Database db)
        {
            var sql = $@"; INSERT INTO Worker (Id, Server) VALUES(@workerId, @serverId);";            
            return db.Execute(sql, new { workerId = workerId, serverId = serverId });           
        }

        public ScheduleEntity LockFirstScheduledItem(Database db)
        {
            var sql = $@";update top (1) s
set s.LastInvocation = GETUTCDATE()
output INSERTED.*
from Schedule s
WHERE s.NextInvocation < GETUTCDATE()";
            return db.Query<ScheduleEntity>(sql).FirstOrDefault();
        }

        public int UpdateScheduledItem(ScheduleEntity scheduleEntity, Expression<Func<ScheduleEntity, object>> fields, Database db)
        {
            return db.Update<ScheduleEntity>(scheduleEntity, fields);
        }

        public int CreateOrUpdateRecurringJob(ScheduledJob job, Database db)
        {
            var sql =
$@"; merge Schedule as Target
using (VALUES(@name, @cron, @lastInvocation, @nextInvocation, @jobInvocationData, @workflowInvocationData)) as Source (Name, Cron, LastInvocation, NextInvocation, JobInvocationData, WorkflowInvocationData)
on Target.Name = Source.Name
when matched then update set Cron = Source.Cron, LastInvocation = Source.LastInvocation, NextInvocation = Source.NextInvocation, JobInvocationData = Source.JobInvocationData, WorkflowInvocationData = Source.WorkflowInvocationData
when not matched then insert(Name, Cron, LastInvocation, NextInvocation, JobInvocationData, WorkflowInvocationData) values(Source.Name,Source.Cron,Source.LastInvocation,Source.NextInvocation,Source.JobInvocationData,Source.WorkflowInvocationData,);
";
            return db.Execute(sql, new { name = job.Name, cron = job.Cron, lastInvocation = job.LastInvocation, nextInvocation = job.NextInvocation, jobInvocationData = ScheduledJobInvocationData.FromScheduledJob(job), workflowInvocationData = "" });

        }        
    }
}
