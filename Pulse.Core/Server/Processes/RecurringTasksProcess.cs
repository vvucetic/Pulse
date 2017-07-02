using NCrontab;
using Pulse.Core.Common;
using Pulse.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server.Processes
{
    public class RecurringTasksProcess : IBackgroundProcess
    {
        private readonly ILog _logger = LogProvider.GetLogger();

        public void Execute(BackgroundProcessContext context)
        {
            var jobsEnqueued = 0;
            while (context.Storage.EnqueueNextScheduledItem((CalculateNextInvocation)))
            {
                jobsEnqueued++;

                if (context.IsShutdownRequested)
                {
                    break;
                }
            };
            if (jobsEnqueued != 0)
            {
                _logger.Log($"{jobsEnqueued} scheduled job(s)/workflow(s) enqueued by Recurring Tasks Process.");
            }
            context.Wait(TimeSpan.FromMinutes(1));
        }

        private ScheduledJob CalculateNextInvocation(ScheduledJob job)
        {
            var schedule = CrontabSchedule.Parse(job.Cron);
            job.NextInvocation = schedule.GetNextOccurrence(DateTime.UtcNow);
            return job;
        }
    }
}
