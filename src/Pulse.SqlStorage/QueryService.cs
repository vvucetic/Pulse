using Dapper;
using Dapper.Contrib.Extensions;
using Pulse.Core.Common;
using Pulse.Core.States;
using Pulse.SqlStorage.Entities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public class QueryService : IQueryService
    {
        private readonly SqlServerStorageOptions _options;
        public QueryService(SqlServerStorageOptions options, ITableNameMapper tableNameMapper = null)
        {
           this._options = options ?? throw new ArgumentNullException(nameof(options));
            tableNameMapper = tableNameMapper ?? new TableNameMapper(options.SchemaName);
            Dapper.Contrib.Extensions.SqlMapperExtensions.InvalidateTableNameCache();
            Dapper.Contrib.Extensions.SqlMapperExtensions.TableNameMapper = tableNameMapper.GetTableName;
        }

        public FetchedJob FetchNextJob(string[] queues, string workerId, DbConnection connection, DbTransaction transaction = null)
        {
            var fetchJobSqlTemplate = $@";
update top (1) q
set q.FetchedAt = GETUTCDATE(), q.WorkerId = @workerId
output INSERTED.Id as QueueJobId, INSERTED.JobId, INSERTED.Queue, INSERTED.FetchedAt, INSERTED.WorkerId
from [{this._options.SchemaName}].Queue q
where q.Queue IN @queues and q.WorkerId is null";
            var fetchedJob = connection.Query<FetchedJob>(
                fetchJobSqlTemplate, 
                new { queues = queues, workerId = workerId }, 
                transaction).FirstOrDefault();
            return fetchedJob;
        }

        public FetchedJob EnqueueNextDelayedJob(DbConnection connection, DbTransaction transaction = null)
        {
            var sql = $@";
DECLARE @Ids table(Id int, [Queue] [nvarchar](50))

update top (1) j
set j.NextRetry = NULL
output INSERTED.Id as Id, inserted.[Queue] INTO @Ids
from [{this._options.SchemaName}].Job j
WHERE j.NextRetry IS NOT NULL AND j.NextRetry < GETUTCDATE()

INSERT [{this._options.SchemaName}].[Queue] (JobId, [Queue])
OUTPUT INSERTED.JobId, INSERTED.[Queue], INSERTED.[FetchedAt]
SELECT [Id], [Queue] FROM @Ids;";
            return connection.Query<FetchedJob>(sql, null, transaction).FirstOrDefault();
        }

        public JobEntity GetJob(int jobId, DbConnection connection, DbTransaction transaction = null)
        {
            return connection.Query<JobEntity>(
                $"SELECT * FROM [{this._options.SchemaName}].Job WHERE Id = @jobId", 
                new { jobId },
                transaction).FirstOrDefault();
        }

        public int InsertJob(JobEntity job, DbConnection connection, DbTransaction transaction = null)
        {
            var id = connection.Insert<JobEntity>(job, transaction);
            return (int)id;
        }

        public bool UpdateJob(JobEntity job, Expression<Func<JobEntity, object>> fields, DbConnection connection, DbTransaction transaction = null)
        {
            return connection.Update<JobEntity>(job, fields, transaction);
        }

        public int InsertJobState(StateEntity state, DbConnection connection, DbTransaction transaction = null)
        {
            var id = connection.Insert<StateEntity>(state, transaction);
            return (int)id;
        }

        public bool SetJobState(int jobId, int stateId, string stateName, DbConnection connection, DbTransaction transaction = null)
        {
            return connection.Update<JobEntity>(
                new JobEntity() { Id = jobId, State = stateName, StateId = stateId }, 
                t => new { t.State, t.StateId }, 
                transaction);
        }      

        public int InsertJobToQueue(int jobId, string queue, DbConnection connection, DbTransaction transaction = null)
        {
            var insertedQueueItem = new QueueEntity()
            {
                JobId = jobId,
                Queue = queue
            };
            connection.Insert<QueueEntity>(insertedQueueItem, transaction);
            return insertedQueueItem.Id;
        }

        public bool RemoveFromQueue(int queueJobId, DbConnection connection, DbTransaction transaction = null)
        {
            return connection.Delete<QueueEntity>(new QueueEntity { Id = queueJobId }, transaction);
        }

        public bool UpdateQueue(QueueEntity queueEntity, Expression<Func<QueueEntity, object>> fields, DbConnection connection, DbTransaction transaction = null)
        {
            return connection.Update<QueueEntity>(queueEntity, fields, transaction);
        }

        public int RemoveTimedOutServers(TimeSpan timeout, DbConnection connection, DbTransaction transaction = null)
        {
            var sql = $@"delete from [{this._options.SchemaName}].Server where LastHeartbeat < @timeoutAt";
            return connection.Execute(
                sql, 
                new { timeoutAt = DateTime.UtcNow.Add(timeout.Negate()) },
                transaction);
        }

        public int HeartbeatServer(string server, string data, DbConnection connection, DbTransaction transaction = null)
        {
            var sql =
$@"; merge [{this._options.SchemaName}].Server as Target
using (VALUES(@server, @data, @heartbeat)) as Source (Id, Data, Heartbeat)
on Target.Id = Source.Id
when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat
when not matched then insert(Id, Data, LastHeartbeat) values(Source.Id, Source.Data, Source.Heartbeat);
            ";
            return connection.Execute(
                sql, 
                new { server = server, data = data, heartbeat = DateTime.UtcNow }, 
                transaction);
            
        }
        public bool RemoveServer(string serverId, DbConnection connection, DbTransaction transaction = null)
        {
            return connection.Delete<ServerEntity>(new ServerEntity { Id = serverId }, transaction);           
        }

        public void RegisterWorker(string workerId, string serverId, DbConnection connection, DbTransaction transaction = null)
        {
            var worker = new WorkerEntity()
            {
                Id = workerId,
                Server = serverId
            };
            connection.Insert<WorkerEntity>(worker, transaction);      
        }

        public ScheduleEntity LockFirstScheduledItem(DbConnection connection, DbTransaction transaction = null)
        {
            var sql = $@";update top (1) s
set s.LastInvocation = GETUTCDATE()
output INSERTED.*
from [{this._options.SchemaName}].Schedule s
WHERE s.NextInvocation < GETUTCDATE() AND (s.OnlyIfLastFinishedOrFailed=0 OR NOT EXISTS(SELECT Id FROM [{this._options.SchemaName}].[Job] j WHERE j.ScheduleName = s.Name AND j.State IN @states))
";
            return connection.Query<ScheduleEntity>(
                sql, 
                new { states = new string[] { ProcessingState.DefaultName, ScheduledState.DefaultName, AwaitingState.DefaultName, EnqueuedState.DefaultName } },
                transaction
                ).FirstOrDefault();
        }

        public ScheduleEntity LockScheduledItem(string name, DbConnection connection, DbTransaction transaction = null)
        {
            var sql = $@";update top (1) s
set s.LastInvocation = GETUTCDATE()
output INSERTED.*
from [{this._options.SchemaName}].Schedule s
WHERE s.Name = @name
";
            return connection.Query<ScheduleEntity>(
                sql, 
                new { name = name }, 
                transaction).FirstOrDefault();
        }

        public bool UpdateScheduledItem(ScheduleEntity scheduleEntity, Expression<Func<ScheduleEntity, object>> fields, DbConnection connection, DbTransaction transaction = null)
        {
            return connection.Update<ScheduleEntity>(scheduleEntity, fields, transaction);
        }

        public int CreateOrUpdateRecurringJob(ScheduledTask scheduledTask, DbConnection connection, DbTransaction transaction = null)
        {
            var sql =
$@"; merge [{this._options.SchemaName}].Schedule as Target
using (VALUES(@name, @cron, @lastInvocation, @nextInvocation, @jobInvocationData, @workflowInvocationData, @onlyIfLastFinishedOrFailed)) as Source (Name, Cron, LastInvocation, NextInvocation, JobInvocationData, WorkflowInvocationData, OnlyIfLastFinishedOrFailed)
on Target.Name = Source.Name
when matched then update set Cron = Source.Cron, LastInvocation = Source.LastInvocation, NextInvocation = Source.NextInvocation, JobInvocationData = Source.JobInvocationData, WorkflowInvocationData = Source.WorkflowInvocationData, OnlyIfLastFinishedOrFailed = Source.OnlyIfLastFinishedOrFailed
when not matched then insert(Name, Cron, LastInvocation, NextInvocation, JobInvocationData, WorkflowInvocationData, OnlyIfLastFinishedOrFailed) values(Source.Name,Source.Cron,Source.LastInvocation,Source.NextInvocation,Source.JobInvocationData,Source.WorkflowInvocationData, Source.OnlyIfLastFinishedOrFailed);
";
            return connection.Execute(
                sql, 
                new { name = scheduledTask.Name, cron = scheduledTask.Cron, lastInvocation = scheduledTask.LastInvocation, nextInvocation = scheduledTask.NextInvocation, jobInvocationData = JobHelper.ToJson(ScheduledJobInvocationData.FromScheduledJob(scheduledTask)), workflowInvocationData = JobHelper.ToJson(scheduledTask.Workflow), onlyIfLastFinishedOrFailed = scheduledTask.OnlyIfLastFinishedOrFailed },
                transaction);
        }

        public bool RemoveScheduledItem(string name, DbConnection connection, DbTransaction transaction = null)
        {
            return connection.Delete<ScheduleEntity>(new ScheduleEntity { Name = name }, transaction);
        }

        public void InsertJobCondition(JobConditionEntity jobCondition, DbConnection connection, DbTransaction transaction = null)
        {
            connection.Insert<JobConditionEntity>(jobCondition, transaction);
        }

        public List<JobEntity> MarkAsFinishedAndGetNextJobs(int jobId, DbConnection connection, DbTransaction transaction = null)
        {
            var sql = $@";DECLARE @Ids table(Id int)
UPDATE jc
   SET jc.[Finished] = 1
      ,jc.[FinishedAt] = GETUTCDATE()
OUTPUT inserted.JobId INTO @Ids 
FROM [{this._options.SchemaName}].JobCondition jc
WHERE jc.ParentJobId = @jobId

SELECT * FROM [{this._options.SchemaName}].Job j
WHERE State = @state AND 
j.Id IN (SELECT * FROM @Ids) AND
NOT EXISTS (SELECT * FROM [{this._options.SchemaName}].JobCondition jc WHERE jc.JobId = j.Id AND jc.Finished = 0)";
            return connection.Query<JobEntity>(
                sql, 
                new { jobId = jobId, state = AwaitingState.DefaultName },
                transaction
                ).ToList();
        }

        /// <summary>
        /// Recursively get all dependent jobs on given job
        /// </summary>
        /// <param name="jobs"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public List<JobEntity> GetDependentWorkflowTree(int jobId, DbConnection connection, DbTransaction transaction = null)
        {
            var sql = $@";with cte as 
(
    select * from [{this._options.SchemaName}].JobCondition where ParentJobId=@jobId
    union all
    select jc.* from cte 
        inner join [{this._options.SchemaName}].JobCondition jc on cte.JobId = jc.ParentJobId
)
SELECT * FROM [{this._options.SchemaName}].Job j WHERE j.Id IN (SELECT JobId FROM cte)";
            return connection.Query<JobEntity>(
                sql, 
                new { jobId = jobId }, 
                transaction
                ).ToList();
        }
    }

    
}
