using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.DataModel
{
    public class StatisticsDto
    {
        public long Servers { get; set; }
        public long Recurring { get; set; }
        public long Enqueued { get; set; }
        public long Queues { get; set; }
        public long Scheduled { get; set; }
        public long Processing { get; set; }
        public long Succeeded { get; set; }
        public long Failed { get; set; }
        public long Deleted { get; set; }
        public long Awaiting { get; set; }
        public long ConsequentlyFailed { get; set; }
    }
}
