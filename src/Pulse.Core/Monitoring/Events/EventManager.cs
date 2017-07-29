using Pulse.Core.Common;
using Pulse.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.Events
{
    public class EventManager
    {
        private readonly EventSubscriber _eventSubscriber = EventSubscriberProvider.GetSubscriber();

        private readonly ILog Logger = LogProvider.GetLogger();

        private IMonitoringApi _monitoringApi;

        public EventManager(IMonitoringApi monitoringApi)
        {
            _monitoringApi = monitoringApi ?? throw new ArgumentNullException(nameof(monitoringApi));
        }

        public void RaiseOnReschedule(QueueJob queueJob, string serverId, string workerId, Exception failException, DateTime nextRun)
        {
            try
            {
                _eventSubscriber.OnReschedule(_monitoringApi, queueJob, serverId, workerId, failException, nextRun);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Error in event subscriber method '{nameof(_eventSubscriber.OnReschedule)}'", ex);
            }
        }

        public void RaiseOnFail(QueueJob queueJob, string serverId, string workerId, Exception failException)
        {
            try
            {
                _eventSubscriber.OnFail(_monitoringApi, queueJob, serverId, workerId, failException);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Error in event subscriber method '{nameof(_eventSubscriber.OnFail)}'", ex);
            }
        }

        public void RaiseOnSuccess(QueueJob queueJob, string serverId, string workerId)
        {
            try
            {
                _eventSubscriber.OnSuccess(_monitoringApi, queueJob, serverId, workerId);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Error in event subscriber method '{nameof(_eventSubscriber.OnSuccess)}'", ex);
            }
        }

        public void RaiseOnProcessing(QueueJob queueJob, string serverId, string workerId)
        {
            try
            {
                _eventSubscriber.OnProcessing(_monitoringApi, queueJob, serverId, workerId);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Error in event subscriber method '{nameof(_eventSubscriber.OnProcessing)}'", ex);
            }
        }
        
    }
}
