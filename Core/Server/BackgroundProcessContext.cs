using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Server
{
    public class BackgroundProcessContext
    {
        public CancellationToken CancellationToken { get; }

        public BackgroundProcessContext(CancellationToken cancellationToken)
        {

        }

        public bool IsShutdownRequested => CancellationToken.IsCancellationRequested;

        public void Wait(TimeSpan timeout)
        {
            CancellationToken.WaitHandle.WaitOne(timeout);
        }
    }
}
