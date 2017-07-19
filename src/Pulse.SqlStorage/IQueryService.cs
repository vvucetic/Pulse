using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NPoco;
using Pulse.Core.Common;
using Pulse.SqlStorage.Entities;

namespace Pulse.SqlStorage
{
    public interface IQueryService
    {
        int CreateOrUpdateRecurringJob(ScheduledTask scheduledTask, Database db);
        FetchedJob EnqueueNextDelayedJob(Database db);
        FetchedJob FetchNextJob(string[] queues, string workerId, Database db);
        List<JobEntity> GetDependentWorkflowTree(int jobId, Database db);
        JobEntity GetJob(int jobId, Database db);
        int HeartbeatServer(string server, string data, Database db);
        int InsertJob(JobEntity job, Database db);
        void InsertJobCondition(JobConditionEntity jobCondition, Database db);
        int InsertJobState(StateEntity state, Database db);
        int InsertJobToQueue(int jobId, string queue, Database db);
        ScheduleEntity LockFirstScheduledItem(Database db);
        ScheduleEntity LockScheduledItem(string name, Database db);
        List<JobEntity> MarkAsFinishedAndGetNextJobs(int jobId, Database db);
        void RegisterWorker(string workerId, string serverId, Database db);
        int RemoveFromQueue(int queueJobId, Database db);
        int RemoveScheduledItem(string name, Database db);
        int RemoveServer(string serverId, Database db);
        int RemoveTimedOutServers(TimeSpan timeout, Database db);
        int SetJobState(int jobId, int stateId, string stateName, Database db);
        int UpdateJob(JobEntity job, Expression<Func<JobEntity, object>> fields, Database db);
        int UpdateQueue(QueueEntity queueEntity, Expression<Func<QueueEntity, object>> fields, Database db);
        int UpdateScheduledItem(ScheduleEntity scheduleEntity, Expression<Func<ScheduleEntity, object>> fields, Database db);
    }
}