using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.DataModel
{
    public class EnqueuedJobDto
    {
        public JobDto JobInfo { get; set; }

        public DateTime? EnqueuedAt { get; set; }
    }
}
