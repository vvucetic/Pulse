using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    internal class FetchedJob
    {
        public int QueueJobId { get; set; }
        public int JobId { get; set; }
        public string Queue { get; set; }
    }
}
