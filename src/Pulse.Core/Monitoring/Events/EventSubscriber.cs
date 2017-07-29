using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.Events
{
    public class EventSubscriber
    {
        public virtual void OnReschedule(IMonitoringApi montoringApi, QueueJob queueJob, string serverId, string workerId, Exception failException, DateTime nextRun) { }

        public virtual void OnFail(IMonitoringApi montoringApi, QueueJob queueJob, string serverId, string workerId, Exception failException) { }

        public virtual void OnSuccess(IMonitoringApi montoringApi, QueueJob queueJob, string serverId, string workerId) { }

        public virtual void OnProcessing(IMonitoringApi montoringApi, QueueJob queueJob, string serverId, string workerId) { }
    
    }
}
