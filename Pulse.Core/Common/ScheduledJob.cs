using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Common
{
    public class ScheduledJob
    {
        public string Name { get; set; }

        public QueueJob QueueJob { get; set; } = new QueueJob();

        public string Cron { get; set; }

        public DateTime LastInvocation { get; set; }

        public DateTime NextInvocation { get; set; }
    }
}
