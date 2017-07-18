using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public class FetchedJob
    {
        public int QueueJobId { get; set; }
        public int JobId { get; set; }
        public string Queue { get; set; }
        public DateTime FetchedAt { get; set; }
        public string WorkerId { get; set; }
    }
}
