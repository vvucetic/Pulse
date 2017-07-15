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

        public bool OnlyIfLastFinishedOrFailed { get; set; }

        public static ScheduledTask ToScheduleTask(ScheduleEntity scheduleEntity)
        {
            if (!string.IsNullOrEmpty(scheduleEntity.JobInvocationData))
            {
                var jobInvocationData = JobHelper.FromJson<ScheduledJobInvocationData>(scheduleEntity.JobInvocationData);

                var scheduledJob = new ScheduledTask()
                {
                    Cron = scheduleEntity.Cron,
                    Name = scheduleEntity.Name,
                    Job = new QueueJob()
                    {
                        Job = jobInvocationData.Job,
                        ContextId = jobInvocationData.ContextId,
                        MaxRetries = jobInvocationData.MaxRetries,
                        QueueName = jobInvocationData.Queue,
                        CreatedAt = DateTime.UtcNow,
                        NextJobs = new List<int>(),
                        RetryCount = 1
                    },
                    LastInvocation = scheduleEntity.LastInvocation,
                    NextInvocation = scheduleEntity.NextInvocation,
                    OnlyIfLastFinishedOrFailed=scheduleEntity.OnlyIfLastFinishedOrFailed
                };

                return scheduledJob;
            }
            else if(!string.IsNullOrEmpty(scheduleEntity.WorkflowInvocationData))
            {
                var workflow = JobHelper.FromJson<Workflow>(scheduleEntity.WorkflowInvocationData);

                var scheduledJob = new ScheduledTask()
                {
                    Cron = scheduleEntity.Cron,
                    Name = scheduleEntity.Name,
                    Workflow = workflow,
                    LastInvocation = scheduleEntity.LastInvocation,
                    NextInvocation = scheduleEntity.NextInvocation,
                    OnlyIfLastFinishedOrFailed = scheduleEntity.OnlyIfLastFinishedOrFailed
                };

                return scheduledJob;
            }
            else
            {
                throw new Exception("Scheduled task does not contain job invocation data or workflow invocation data");
            }
        }
    }
}
