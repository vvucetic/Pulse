using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Events
{
    public class EventSubscriber
    {
        public virtual void OnReschedule(QueueJob queueJob, string serverId, string workerId, Exception failException, DateTime nextRun) { }

        public virtual void OnFail(QueueJob queueJob, string serverId, string workerId, Exception failException) { }

        public virtual void OnSuccess(QueueJob queueJob, string serverId, string workerId) { }

        public virtual void OnProcessing(QueueJob queueJob, string serverId, string workerId) { }
    
    }
}
