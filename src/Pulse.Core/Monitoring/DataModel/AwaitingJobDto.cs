using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.DataModel
{
    public class AwaitingJobDto
    {
        public JobDto JobInfo { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
