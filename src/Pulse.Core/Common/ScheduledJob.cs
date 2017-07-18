using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Common
{
    public class ScheduledTask
    {
        public string Name { get; set; }

        public QueueJob Job { get; set; }

        public Workflow Workflow { get; set; }

        public string Cron { get; set; }

        public DateTime LastInvocation { get; set; }

        public DateTime NextInvocation { get; set; }

        public bool OnlyIfLastFinishedOrFailed { get; set; }

    }
}
