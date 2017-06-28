using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage.Entities
{
    [TableName("Job")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class JobEntity
    {
        public JobEntity()
        {
            this.MaxRetries = 10;
            this.RetryCount = 1;
        }
        public int Id { get; set; }
        
        public string State { get; set; }

        public int? StateId { get; set; }

        public string InvocationData { get; set; }

        //public string Arguments { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ExpireAt { get; set; }

        public string NextJobs { get; set; }

        public Guid? ContextId { get; set; }

        public int NumberOfConditionJobs { get; set; }

        public int MaxRetries { get; set; }

        public int RetryCount { get; set; }

        public DateTime? NextRetry { get; set; }
    }
}
