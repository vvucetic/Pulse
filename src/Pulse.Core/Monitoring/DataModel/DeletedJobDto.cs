using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.DataModel
{
    public class DeletedJobDto
    {
        public JobDto JobInfo { get; set; }

        public DateTime DeletedAt { get; set; }
    }
}
