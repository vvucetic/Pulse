using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    public class BackgroundProcessContext
    {
        public CancellationToken CancellationToken { get; }

        public DataStorage Storage { get; }

        public string ServerId { get; }

        public BackgroundProcessContext(string serverId, CancellationToken cancellationToken, DataStorage storage)
        {
            this.ServerId = serverId;
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
