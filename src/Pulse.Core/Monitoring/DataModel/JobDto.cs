using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.DataModel
{
    public class JobDto
    {
        public int JobId { get; set; }

        public Guid? ContextId { get; set; }

        public Job Job { get; set; }

        public DateTime CreatedAt { get; set; }

        public string ScheduleName { get; set; }

        public string Description { get; set; }

        public string State { get; set; }

        public int? StateId { get; set; }

        public string StateReason { get; set; }

        public Guid? WorkflowId { get; set; }
    }
    
}
