using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.DataModel
{
    public class SucceededJobDto
    {
        public JobDto JobInfo { get; set; }

        public object Result { get; set; }

        public long? TotalDuration { get; set; }

        public DateTime? SucceededAt { get; set; }
    }
}
