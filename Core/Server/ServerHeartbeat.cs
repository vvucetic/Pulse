﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    internal class ServerHeartbeat : IBackgroundProcess
    {
        public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(30);

        private readonly TimeSpan _heartbeatInterval;

        public ServerHeartbeat(TimeSpan heartbeatInterval)
        {
            _heartbeatInterval = heartbeatInterval;
        }

        public void Execute(BackgroundProcessContext context)
        {
            //context.storage.HeartBeat.

            context.Wait(_heartbeatInterval);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
