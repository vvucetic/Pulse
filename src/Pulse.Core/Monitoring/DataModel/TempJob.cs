using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.DataModel
{
    public class TempJob
    {
        public int Id { get; set; }

        public Guid? ContextId { get; set; }

        public string InvocationData { get; set; }

        public DateTime CreatedAt { get; set; }

        public string ScheduleName { get; set; }

        public string Description { get; set; }

        public string State { get; set; }

        public int? StateId { get; set; }

        public string StateReason { get; set; }

        public string StateData { get; set; }

        public Guid? WorkflowId { get; set; }
        
    }
}
