using NPoco;
using Pulse.Core.Common;
using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage.Entities
{
    [TableName("Schedule")]
    [PrimaryKey("Name", AutoIncrement = false)]
    public class ScheduleEntity
    {
        public string Name { get; set; }

        public string Cron { get; set; }

        public DateTime LastInvocation { get; set; }

        public DateTime NextInvocation { get; set; }

        public string JobInvocationData { get; set; }

        public string WorkflowInvocationData { get; set; }

        public static ScheduledJob ToScheduleJob(ScheduleEntity scheduleEntity)
        {
            var jobInvocationData = JobHelper.FromJson<ScheduledJobInvocationData>(scheduleEntity.JobInvocationData);

            var scheduledJob = new ScheduledJob()
            {
                Cron = scheduleEntity.Cron,
                Name = scheduleEntity.Name,
                QueueJob = new QueueJob()
                {
                    Job = JobHelper.FromJson<InvocationData>(jobInvocationData.InvocationData).Deserialize(),
                    ContextId = jobInvocationData.ContextId,
                    MaxRetries = jobInvocationData.MaxRetries,
                    QueueName = jobInvocationData.Queue,
                    NumberOfConditionJobs = 0,
                    CreatedAt = DateTime.UtcNow,
                    NextJobs = new List<int>(),
                    RetryCount = 1
                },
                LastInvocation = scheduleEntity.LastInvocation,
                NextInvocation = scheduleEntity.NextInvocation
            };

            return scheduledJob;
        }
    }
}
