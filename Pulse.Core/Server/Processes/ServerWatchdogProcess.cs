using Pulse.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server.Processes
{
    public class ServerWatchdogProcess : IBackgroundProcess
    {
        public static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan DefaultServerTimeout = TimeSpan.FromMinutes(5);
        private readonly ILog _logger = LogProvider.GetLogger();

        private readonly TimeSpan _checkInterval;
        private readonly TimeSpan _serverTimeout;

        public ServerWatchdogProcess(TimeSpan checkInterval, TimeSpan serverTimeout)
        {
            _checkInterval = checkInterval;
            _serverTimeout = serverTimeout;
        }
        public void Execute(BackgroundProcessContext context)
        {
            var serversRemoved = context.Storage.RemoveTimedOutServers(_serverTimeout);
            if (serversRemoved != 0)
            {
                _logger.Log(LogLevel.Information,$"{serversRemoved} servers were removed due to timeout");
            }

            context.Wait(_checkInterval);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
