using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.DataModel
{
    public class ProcessingJobDto
    {
        public JobDto JobInfo { get; set; }

        public DateTime StartedAt { get; set; }

        public string ServerId { get; set; }

        public string WorkerId { get; set; }
    }
}
