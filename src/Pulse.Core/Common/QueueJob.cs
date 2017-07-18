using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Common
{
    public class QueueJob
    {
        public int JobId { get; set; }

        public int QueueJobId { get; set; }

        public string QueueName { get; set; }

        public List<int> NextJobs { get; set; } = new List<int>();

        public Guid? ContextId { get; set; }
        
        public Job Job { get; set; }

        public DateTime? FetchedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ExpireAt { get; set; }

        public int MaxRetries { get; set; } = 10;

        public int RetryCount { get; set; }

        public DateTime? NextRetry { get; set; }

        public string WorkerId { get; set; }

        public Guid? WorkflowId { get; set; }

        public string ScheduleName { get; set; }

        public string Description { get; set; }
    }
}
