using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server.Processes
{
    internal class ServerHeartbeatProcess : IBackgroundProcess
    {
        public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(60);

        private readonly TimeSpan _heartbeatInterval;

        public ServerHeartbeatProcess(TimeSpan heartbeatInterval)
        {
            _heartbeatInterval = heartbeatInterval;
        }

        public void Execute(BackgroundProcessContext context)
        {
            context.Storage.HeartbeatServer(context.ServerId, JobHelper.ToJson(context.ServerContext));
            
            context.Wait(_heartbeatInterval);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
