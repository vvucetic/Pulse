using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common
{
    public class QueueJob
    {
        public int JobId { get; set; }

        public int QueueJobId { get; set; }

        public string QueueName { get; set; }

        public List<int> NextJobs { get; set; } = new List<int>();

        public Guid? ContextId { get; set; }

        public int NumberOfConditionJobs { get; set; }

        public Job Job { get; set; }

        public DateTime? FetchedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
