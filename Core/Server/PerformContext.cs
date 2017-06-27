using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    public class PerformContext
    {
        public PerformContext(CancellationToken cancellationToken, QueueJob queueJob)
        {
            this.CancellationToken = cancellationToken;
            this.QueueJob = queueJob;
        }
        public CancellationToken CancellationToken { get; }

        public QueueJob QueueJob { get; }
    }
}
