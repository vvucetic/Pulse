using Pulse.Core.Server.Processes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    public sealed class BackgroundProcessingServerOptions
    {
        public BackgroundProcessingServerOptions()
        {
            //ShutdownTimeout = BackgroundProcessingServer.DefaultShutdownTimeout;
            HeartbeatInterval = ServerHeartbeatProcess.DefaultHeartbeatInterval;
            ServerCheckInterval = ServerWatchdogProcess.DefaultCheckInterval;
            ServerTimeout = ServerWatchdogProcess.DefaultServerTimeout;
        }
        public TimeSpan ShutdownTimeout { get; set; }
        public TimeSpan HeartbeatInterval { get; set; }
        public TimeSpan ServerCheckInterval { get; set; }
        public TimeSpan ServerTimeout { get; set; }
        public string ServerName { get; set; }
        public ServerContext ServerContext { get; set; }
    }
}
