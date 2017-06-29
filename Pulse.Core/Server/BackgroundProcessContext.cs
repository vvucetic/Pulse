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

        public IReadOnlyDictionary<string, object> Properties { get; }

        public ServerContext ServerContext { get; }

        public BackgroundProcessContext(string serverId, CancellationToken cancellationToken, DataStorage storage, IDictionary<string, object> properties, ServerContext serverContext)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            this.ServerId = serverId ?? throw new ArgumentNullException(nameof(serverId));
            this.Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.ServerContext = serverContext ?? throw new ArgumentNullException(nameof(serverContext));
            this.Properties = new Dictionary<string, object>(properties, StringComparer.OrdinalIgnoreCase);
            this.CancellationToken = cancellationToken;

        }

        public bool IsShutdownRequested => CancellationToken.IsCancellationRequested;

        public void Wait(TimeSpan timeout)
        {
            CancellationToken.WaitHandle.WaitOne(timeout);
        }
    }
}
