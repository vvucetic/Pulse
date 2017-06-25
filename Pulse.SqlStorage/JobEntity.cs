using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public class JobEntity
    {
        public int Id { get; set; }
        
        public string State { get; set; }

        public string InvocationData { get; set; }

        public string Arguments { get; set; }

        public DateTime CreatedAt { get; set; }

        public string NextJobs { get; set; }

        public Guid ContextId { get; set; }

        public int NumberOfConditionJobs { get; set; }
    
    }
}
