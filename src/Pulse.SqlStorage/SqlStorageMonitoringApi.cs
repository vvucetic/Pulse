using Pulse.Core.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulse.Core.Monitoring.DataModel;
using Pulse.SqlStorage.Entities;
using Pulse.Core.Common;
using Pulse.Core.Storage;
using Pulse.Core.States;
using Dapper;

namespace Pulse.SqlStorage
{
    public class SqlStorageMonitoringApi : IMonitoringApi
    {
        private readonly SqlStorage _storage;
        public SqlStorageMonitoringApi(SqlStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }
        public List<SucceededJobDto> GetSucceededJobs(int from, int count)
        {
            return GetJobs<SucceededJobDto>(SucceededState.DefaultName, from, count, (job, stateData) =>
            {
                return new SucceededJobDto()
                {
                    JobInfo = job,
                    Result = stateData.ContainsKey("Result") ? stateData["Result"] : null,
                    SucceededAt = JobHelper.DeserializeDateTime(stateData["SucceededAt"]),
                    TotalDuration = stateData.ContainsKey("PerformanceDuration") && stateData.ContainsKey("Latency")
                        ? (long?)long.Parse(stateData["PerformanceDuration"]) + (long?)long.Parse(stateData["Latency"])
                        : null
                };
            });
        }

        public List<FailedJobDto> GetFailedJobs(int from, int count)
        {
            return GetJobs<FailedJobDto>(FailedState.DefaultName, from, count, (job, stateData) =>
            {
                return new FailedJobDto()
                {
                    JobInfo = job,
                    Reason = job.StateReason,
                    ExceptionDetails = stateData["ExceptionDetails"],
                    ExceptionMessage = stateData["ExceptionMessage"],
                    ExceptionType = stateData["ExceptionType"],
                    FailedAt = JobHelper.DeserializeNullableDateTime(stateData["FailedAt"])
                };
            });
        }

        public List<ScheduledJobDto> GetScheduledJobs(int from, int count)
        {
            return GetJobs<ScheduledJobDto>(ScheduledState.DefaultName, from, count, (job, stateData) =>
            {
                return new ScheduledJobDto()
                {
                    JobInfo = job,
                    EnqueueAt = JobHelper.DeserializeDateTime(stateData["EnqueueAt"]),
                    ScheduledAt = JobHelper.DeserializeDateTime(stateData["ScheduledAt"])
                };
            });
        }

        public List<DeletedJobDto> GetDeletedJobs(int from, int count)
        {
            return GetJobs<DeletedJobDto>(DeletedState.DefaultName, from, count, (job, stateData) =>
            {
                return new DeletedJobDto()
                {
                    JobInfo = job,
                    DeletedAt = JobHelper.DeserializeDateTime(stateData["DeletedAt"])
                };
            });
        }

        public List<ConsequentlyFailedJobDto> GetConsequentlyFailedJobs(int from, int count)
        {
            return GetJobs<ConsequentlyFailedJobDto>(ConsequentlyFailed.DefaultName, from, count, (job, stateData) =>
            {
                return new ConsequentlyFailedJobDto()
                {
                    JobInfo = job,
                    FailedParentId = int.Parse(stateData["FailedParentId"]),
                    Reason = stateData["Reason"],
                    FailedAt = JobHelper.DeserializeDateTime(stateData["FailedAt"])
                };
            });
        }

        public List<AwaitingJobDto> GetAwaitingJobs(int from, int count)
        {
            return GetJobs<AwaitingJobDto>(AwaitingState.DefaultName, from, count, (job, stateData) =>
            {
                return new AwaitingJobDto()
                {
                    JobInfo = job,
                    CreatedAt = JobHelper.DeserializeDateTime(stateData["CreatedAt"])
                };
            });
        }

        public List<EnqueuedJobDto> GetEnqueuedJobs(int from, int count)
        {
            return GetJobs<EnqueuedJobDto>(EnqueuedState.DefaultName, from, count, (job, stateData) =>
            {
                return new EnqueuedJobDto()
                {
                    JobInfo = job,
                    EnqueuedAt = JobHelper.DeserializeDateTime(stateData["EnqueuedAt"])
                };
            });
        }

        public List<ProcessingJobDto> GetProcessingJobs(int from, int count)
        {
            return GetJobs<ProcessingJobDto>(ProcessingState.DefaultName, from, count, (job, stateData) =>
            {
                return new ProcessingJobDto()
                {
                    JobInfo = job,
                    StartedAt = JobHelper.DeserializeDateTime(stateData["StartedAt"]),
                    ServerId = stateData["ServerId"],
                    WorkerId = stateData["WorkerId"]
                };
            });
        }

        public StatisticsDto GetStatistics()
        {
            string sql = $@"
set transaction isolation level read committed;
select count(Id) from [{_storage._options.SchemaName}].Job with (nolock) where StateName = N'{EnqueuedState.DefaultName}';
select count(Id) from [{_storage._options.SchemaName}].Job with (nolock) where StateName = N'{FailedState.DefaultName}';
select count(Id) from [{_storage._options.SchemaName}].Job with (nolock) where StateName = N'{ProcessingState.DefaultName}';
select count(Id) from [{_storage._options.SchemaName}].Job with (nolock) where StateName = N'{ScheduledState.DefaultName}';
select count(Id) from [{_storage._options.SchemaName}].Job with (nolock) where StateName = N'{ConsequentlyFailed.DefaultName}';
select count(Id) from [{_storage._options.SchemaName}].Job with (nolock) where StateName = N'{AwaitingState.DefaultName}';
select count(Id) from [{_storage._options.SchemaName}].Server with (nolock);

";
            //using (var db = _storage.GetDatabase())
            //{
            //    db.FetchMultiple<long, long, long, long>
            //}
            //    var statistics = UseConnection(connection =>
            //    {
            //        var stats = new StatisticsDto();
            //        using (var multi = connection.QueryMultiple(sql, commandTimeout: _storage.CommandTimeout))
            //        {
            //            stats.Enqueued = multi.ReadSingle<int>();
            //            stats.Failed = multi.ReadSingle<int>();
            //            stats.Processing = multi.ReadSingle<int>();
            //            stats.Scheduled = multi.ReadSingle<int>();

            //            stats.Servers = multi.ReadSingle<int>();

            //            stats.Succeeded = multi.ReadSingleOrDefault<long?>() ?? 0;
            //            stats.Deleted = multi.ReadSingleOrDefault<long?>() ?? 0;

            //            stats.Recurring = multi.ReadSingle<int>();
            //        }
            //        return stats;
            //    });

            //statistics.Queues = _storage.QueueProviders
            //    .SelectMany(x => x.GetJobQueueMonitoringApi().GetQueues())
            //    .Count();

            return null;
        }

        private List<T> GetJobs<T>(string stateName, int from, int count, Func<JobDto, Dictionary<string, string>, T> map)
        {
            var sql = $@";
with cte as
(
  select j.Id, row_number() over(order by j.Id desc) as row_num
  from[{ _storage._options.SchemaName}].Job j with(nolock)
  where j.State = @stateName
)
select j.*, s.Reason as StateReason, s.Data as StateData
from[{_storage._options.SchemaName}].Job j with(nolock)
inner join cte on cte.Id = j.Id
left join[{_storage._options.SchemaName}].State s with(nolock) on j.StateId = s.Id
where cte.row_num between @start and @end
order by j.Id desc
";
            return _storage.UseConnection((conn) =>
            {
                return conn.Query<TempJob>(sql, new { stateName = stateName, start = @from + 1, end = @from + count })
                    .ToList()
                    .Select(t => map(
                        new JobDto()
                        {
                            ContextId = t.ContextId,
                            CreatedAt = t.CreatedAt,
                            Description = t.Description,
                            Job = JobHelper.FromJson<InvocationData>(t.InvocationData).Deserialize(),
                            JobId = t.Id,
                            ScheduleName = t.ScheduleName,
                            State = t.State,
                            StateId = t.StateId,
                            WorkflowId = t.WorkflowId,
                            StateReason = t.StateReason
                        },
                        ParseStateData(t.StateData)))
                    .ToList();
            });
        }
    

        private Dictionary<string, string> ParseStateData(string data)
        {
            var deserializedData = JobHelper.FromJson<Dictionary<string, string>>(data);
            var stateData = deserializedData != null
                ? new Dictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                : null;
            return stateData;
        }
        
    }
}
