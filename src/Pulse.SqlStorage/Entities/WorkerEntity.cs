using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage.Entities
{
    public class WorkerEntity
    {
        [ExplicitKey]
        public string Id { get; set; }

        public string Server { get; set; }
    }
}
