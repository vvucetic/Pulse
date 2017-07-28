using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Pulse.Core.Common;
using Pulse.SqlStorage.Entities;
using System.Data.Common;

namespace Pulse.SqlStorage
{
    public interface IQueryService
    {
        int CreateOrUpdateRecurringJob(ScheduledTask scheduledTask, DbConnection connection, DbTransaction transaction = null);
        FetchedJob EnqueueNextDelayedJob(DbConnection connection, DbTransaction transaction = null);
        FetchedJob FetchNextJob(string[] queues, string workerId, DbConnection connection, DbTransaction transaction = null);
        List<JobEntity> GetDependentWorkflowTree(int jobId, DbConnection connection, DbTransaction transaction = null);
        JobEntity GetJob(int jobId, DbConnection connection, DbTransaction transaction = null);
        int HeartbeatServer(string server, string data, DbConnection connection, DbTransaction transaction = null);
        int InsertJob(JobEntity job, DbConnection connection, DbTransaction transaction = null);
        void InsertJobCondition(JobConditionEntity jobCondition, DbConnection connection, DbTransaction transaction = null);
        int InsertJobState(StateEntity state, DbConnection connection, DbTransaction transaction = null);
        int InsertJobToQueue(int jobId, string queue, DbConnection connection, DbTransaction transaction = null);
        ScheduleEntity LockFirstScheduledItem(DbConnection connection, DbTransaction transaction = null);
        ScheduleEntity LockScheduledItem(string name, DbConnection connection, DbTransaction transaction = null);
        List<JobEntity> MarkAsFinishedAndGetNextJobs(int jobId, DbConnection connection, DbTransaction transaction = null);
        void RegisterWorker(string workerId, string serverId, DbConnection connection, DbTransaction transaction = null);
        bool RemoveFromQueue(int queueJobId, DbConnection connection, DbTransaction transaction = null);
        bool RemoveScheduledItem(string name, DbConnection connection, DbTransaction transaction = null);
        bool RemoveServer(string serverId, DbConnection connection, DbTransaction transaction = null);
        int RemoveTimedOutServers(TimeSpan timeout, DbConnection connection, DbTransaction transaction = null);
        bool SetJobState(int jobId, int stateId, string stateName, DbConnection connection, DbTransaction transaction = null);
        bool UpdateJob(JobEntity job, Expression<Func<JobEntity, object>> fields, DbConnection connection, DbTransaction transaction = null);
        bool UpdateQueue(QueueEntity queueEntity, Expression<Func<QueueEntity, object>> fields, DbConnection connection, DbTransaction transaction = null);
        bool UpdateScheduledItem(ScheduleEntity scheduleEntity, Expression<Func<ScheduleEntity, object>> fields, DbConnection connection, DbTransaction transaction = null);
    }
}