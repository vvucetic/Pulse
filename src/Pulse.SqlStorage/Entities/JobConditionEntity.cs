
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace Pulse.SqlStorage.Entities
{
    public class JobConditionEntity
    {
        [ExplicitKey]
        public int JobId { get; set; }

        [ExplicitKey]
        public int ParentJobId { get; set; }

        public bool Finished { get; set; }

        public DateTime? FinishedAt { get; set; }
    }
}
