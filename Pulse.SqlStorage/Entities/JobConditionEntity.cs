
using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage.Entities
{
    [TableName("JobCondition")]
    [PrimaryKey(new[] { "JobId", "LinkedJobId" }, AutoIncrement = false)]
    public class JobConditionEntity
    {
        public int JobId { get; set; }

        public int LinkedJobId { get; set; }

        public bool Finished { get; set; }

        public DateTime? FinishedAt { get; set; }
    }
}
