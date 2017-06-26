using Core.Storage;
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

        public DataStorage Storage { get; }

        public BackgroundProcessContext(CancellationToken cancellationToken, DataStorage storage)
        {
            this.CancellationToken = cancellationToken;
            this.Storage = storage;
        }

        public bool IsShutdownRequested => CancellationToken.IsCancellationRequested;

        public void Wait(TimeSpan timeout)
        {
            CancellationToken.WaitHandle.WaitOne(timeout);
        }
    }
}
