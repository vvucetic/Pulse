using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    public class ServerContext
    {
        public ServerContext()
        {
            Queues = new string[0];
            WorkerCount = -1;
        }

        public int WorkerCount { get; set; }
        public string[] Queues { get; set; }
        public DateTime ServerStartedAt { get; set; }
    }
}
