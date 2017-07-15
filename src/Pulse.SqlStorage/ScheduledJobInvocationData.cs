using Pulse.Core.Common;
using Pulse.Core.Storage;
using Pulse.SqlStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public class ScheduledJobInvocationData
    {
        public string Queue { get; set; }

        public Guid? ContextId { get; set; }

        public int MaxRetries { get; set; } = 10;

        public Job Job { get; set; }
                

        public static ScheduledJobInvocationData FromQueueJob(QueueJob queueJob)
        {
            if (queueJob == null)
                return null;
            return new ScheduledJobInvocationData()
            {
                ContextId = queueJob.ContextId,
                Job = queueJob.Job,
                MaxRetries = queueJob.MaxRetries,
                Queue = queueJob.QueueName
            };
        }

        public static ScheduledJobInvocationData FromScheduledJob(ScheduledTask job)
        {
            return FromQueueJob(job.Job);
        }
    }
}
