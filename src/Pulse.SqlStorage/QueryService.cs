using NPoco;
using NPoco.FluentMappings;
using Pulse.Core.Common;
using Pulse.Core.States;
using Pulse.SqlStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public class QueryService : IQueryService
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
output INSERTED.Id as QueueJobId, INSERTED.JobId, INSERTED.Queue, INSERTED.FetchedAt, INSERTED.WorkerId
from [{this._options.SchemaName}].Queue q
where q.Queue IN (@queues) and q.WorkerId is null";
            var fetchedJob = db.Query<FetchedJob>(fetchJobSqlTemplate, new { queues = queues, workerId = workerId }).FirstOrDefault();
            return fetchedJob;
        }

        public FetchedJob EnqueueNextDelayedJob(Database db)
        {
            var sql = $@";
DECLARE @@Ids table(Id int, [Queue] [nvarchar](50))

update top (1) j
set j.NextRetry = NULL
output INSERTED.Id as Id, inserted.[Queue] INTO @@Ids
from [{this._options.SchemaName}].Job j
WHERE j.NextRetry IS NOT NULL AND j.NextRetry < GETUTCDATE()

INSERT [{this._options.SchemaName}].[Queue] (JobId, [Queue])
OUTPUT INSERTED.JobId, INSERTED.[Queue], INSERTED.[FetchedAt]
SELECT [Id], [Queue] FROM @@Ids;";
            return db.Query<FetchedJob>(sql).FirstOrDefault();
        }

        public JobEntity GetJob(int jobId, Database db)
        {
            return db.Query<JobEntity>($"SELECT * FROM [{this._options.SchemaName}].Job WHERE Id = @0", jobId).FirstOrDefault();
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
            //TODO Rewrite with merge otherwise it wont work when successful and queue item is not there
            return db.Update<QueueEntity>(queueEntity, fields);
        }

        public int RemoveTimedOutServers(TimeSpan timeout, Database db)
        {
            var sql = $@"delete from [{this._options.SchemaName}].Server where LastHeartbeat < @timeoutAt";
            return db.Execute(sql, new { timeoutAt = DateTime.UtcNow.Add(timeout.Negate()) });
        }

        public int HeartbeatServer(string server, string data, Database db)
        {
            var sql =
$@"; merge [{this._options.SchemaName}].Server as Target
using (VALUES(@server, @data, @heartbeat)) as Source (Id, Data, Heartbeat)
on Target.Id = Source.Id
when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat
when not matched then insert(Id, Data, LastHeartbeat) values(Source.Id, Source.Data, Source.Heartbeat);
            ";
            return db.Execute(sql, new { server = server, data = data, heartbeat = DateTime.UtcNow });
            
        }
        public int RemoveServer(string serverId, Database db)
        {
            return db.Delete<ServerEntity>((object)serverId);           
        }

        public void RegisterWorker(string workerId, string serverId, Database db)
        {
            var worker = new WorkerEntity()
            {
                Id = workerId,
                Server = serverId
            };
            db.Insert<WorkerEntity>(worker);      
        }

        public ScheduleEntity LockFirstScheduledItem(Database db)
        {
            var sql = $@";update top (1) s
set s.LastInvocation = GETUTCDATE()
output INSERTED.*
from [{this._options.SchemaName}].Schedule s
WHERE s.NextInvocation < GETUTCDATE() AND (s.OnlyIfLastFinishedOrFailed=0 OR NOT EXISTS(SELECT Id FROM [{this._options.SchemaName}].[Job] j WHERE j.ScheduleName = s.Name AND j.State IN (@states)))
";
            return db.Query<ScheduleEntity>(sql, new { states = new string[] { ProcessingState.DefaultName, ScheduledState.DefaultName, AwaitingState.DefaultName, EnqueuedState.DefaultName } }).FirstOrDefault();
        }

        public ScheduleEntity LockScheduledItem(string name, Database db)
        {
            var sql = $@";update top (1) s
set s.LastInvocation = GETUTCDATE()
output INSERTED.*
from [{this._options.SchemaName}].Schedule s
WHERE s.Name = @name
";
            return db.Query<ScheduleEntity>(sql, new { name = name }).FirstOrDefault();
        }

        public int UpdateScheduledItem(ScheduleEntity scheduleEntity, Expression<Func<ScheduleEntity, object>> fields, Database db)
        {
            return db.Update<ScheduleEntity>(scheduleEntity, fields);
        }

        public int CreateOrUpdateRecurringJob(ScheduledTask scheduledTask, Database db)
        {
            var sql =
$@"; merge [{this._options.SchemaName}].Schedule as Target
using (VALUES(@name, @cron, @lastInvocation, @nextInvocation, @jobInvocationData, @workflowInvocationData, @onlyIfLastFinishedOrFailed)) as Source (Name, Cron, LastInvocation, NextInvocation, JobInvocationData, WorkflowInvocationData, OnlyIfLastFinishedOrFailed)
on Target.Name = Source.Name
when matched then update set Cron = Source.Cron, LastInvocation = Source.LastInvocation, NextInvocation = Source.NextInvocation, JobInvocationData = Source.JobInvocationData, WorkflowInvocationData = Source.WorkflowInvocationData, OnlyIfLastFinishedOrFailed = Source.OnlyIfLastFinishedOrFailed
when not matched then insert(Name, Cron, LastInvocation, NextInvocation, JobInvocationData, WorkflowInvocationData, OnlyIfLastFinishedOrFailed) values(Source.Name,Source.Cron,Source.LastInvocation,Source.NextInvocation,Source.JobInvocationData,Source.WorkflowInvocationData, Source.OnlyIfLastFinishedOrFailed);
";
            return db.Execute(sql, new { name = scheduledTask.Name, cron = scheduledTask.Cron, lastInvocation = scheduledTask.LastInvocation, nextInvocation = scheduledTask.NextInvocation, jobInvocationData = JobHelper.ToJson(ScheduledJobInvocationData.FromScheduledJob(scheduledTask)), workflowInvocationData = JobHelper.ToJson(scheduledTask.Workflow), onlyIfLastFinishedOrFailed = scheduledTask.OnlyIfLastFinishedOrFailed });
        }

        public int RemoveScheduledItem(string name, Database db)
        {
            return db.Delete<ScheduleEntity>(name);
        }

        public void InsertJobCondition(JobConditionEntity jobCondition, Database db)
        {
            db.Insert<JobConditionEntity>(jobCondition);
        }

        public List<JobEntity> MarkAsFinishedAndGetNextJobs(int jobId, Database db)
        {
            var sql = $@";DECLARE @@Ids table(Id int)
UPDATE jc
   SET jc.[Finished] = 1
      ,jc.[FinishedAt] = GETUTCDATE()
OUTPUT inserted.JobId INTO @@Ids 
FROM [{this._options.SchemaName}].JobCondition jc
WHERE jc.ParentJobId = @jobId

SELECT * FROM [{this._options.SchemaName}].Job j
WHERE State = @state AND 
j.Id IN (SELECT * FROM @@Ids) AND
NOT EXISTS (SELECT * FROM [{this._options.SchemaName}].JobCondition jc WHERE jc.JobId = j.Id AND jc.Finished = 0)";
            return db.Query<JobEntity>(sql, new { jobId = jobId, state = AwaitingState.DefaultName }).ToList();
        }

        /// <summary>
        /// Recursively get all dependent jobs on given job
        /// </summary>
        /// <param name="jobs"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public List<JobEntity> GetDependentWorkflowTree(int jobId, Database db)
        {
            var sql = $@";with cte as 
(
    select * from [{this._options.SchemaName}].JobCondition where ParentJobId=@jobId
    union all
    select t.* from cte 
        inner join [{this._options.SchemaName}].JobCondition jc on cte.JobId = jc.ParentJobId
)
SELECT * FROM [{this._options.SchemaName}].Job j WHERE j.Id IN (SELECT JobId FROM cte)";
            return db.Query<JobEntity>(sql, new { jobId = jobId }).ToList();
        }
    }

    
}
