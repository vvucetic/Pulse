using Pulse.Core.Common;
using Pulse.Core.Monitoring.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring
{
    public interface IMonitoringApi
    {
        List<SucceededJobDto> GetSucceededJobs(int from, int count);

        List<FailedJobDto> GetFailedJobs(int from, int count);

        List<ScheduledJobDto> GetScheduledJobs(int from, int count);

        List<DeletedJobDto> GetDeletedJobs(int from, int count);

        List<ConsequentlyFailedJobDto> GetConsequentlyFailedJobs(int from, int count);

        List<AwaitingJobDto> GetAwaitingJobs(int from, int count);

        List<EnqueuedJobDto> GetEnqueuedJobs(int from, int count);

        List<ProcessingJobDto> GetProcessingJobs(int from, int count);
    }
}
