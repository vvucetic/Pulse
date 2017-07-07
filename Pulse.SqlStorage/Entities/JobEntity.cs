using NPoco;
using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage.Entities
{
    [TableName("Job")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class JobEntity
    {
        public JobEntity()
        {
            this.MaxRetries = 10;
            this.RetryCount = 1;
        }
        public int Id { get; set; }
        
        public string State { get; set; }

        public int? StateId { get; set; }

        public string InvocationData { get; set; }

        //public string Arguments { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ExpireAt { get; set; }

        public string NextJobs { get; set; }

        public Guid? ContextId { get; set; }
        
        public int MaxRetries { get; set; }

        public int RetryCount { get; set; }

        public DateTime? NextRetry { get; set; }

        public string Queue { get; set; }

        public Guid? WorkflowId { get; set; }

        public string ScheduleName { get; set; }

        public static JobEntity FromScheduleEntity(ScheduledTask scheduledJob)
        {
            return new JobEntity
            {
                ContextId = scheduledJob.Job.ContextId,
                CreatedAt = scheduledJob.Job.CreatedAt,
                ExpireAt = scheduledJob.Job.ExpireAt,
                Id = scheduledJob.Job.JobId,
                MaxRetries = scheduledJob.Job.MaxRetries,
                InvocationData = JobHelper.ToJson(Pulse.Core.Storage.InvocationData.Serialize(scheduledJob.Job.Job)),
                NextJobs = JobHelper.ToJson(scheduledJob.Job.NextJobs),
                RetryCount = 1,
                NextRetry = scheduledJob.Job.NextRetry,
                Queue = scheduledJob.Job.QueueName
            };
        }
    }
}
