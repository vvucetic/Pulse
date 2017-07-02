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

        public string InvocationData { get; set; }
                

        public static ScheduledJobInvocationData FromQueueJob(QueueJob job)
        {
            return new ScheduledJobInvocationData()
            {
                ContextId = job.ContextId,
                InvocationData = JobHelper.ToJson(Pulse.Core.Storage.InvocationData.Serialize(job.Job)),
                MaxRetries = job.MaxRetries,
                Queue = job.QueueName
            };
        }

        public static ScheduledJobInvocationData FromScheduledJob(ScheduledJob job)
        {
            return FromQueueJob(job.QueueJob);
        }
    }
}
