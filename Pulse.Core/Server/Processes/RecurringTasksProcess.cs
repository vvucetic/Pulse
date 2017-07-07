using NCrontab;
using Pulse.Core.Common;
using Pulse.Core.Log;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
            try
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
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 1205)
            {
                //deadlock, just skip, other process on other server is taking care of it...
            }
            context.Wait(TimeSpan.FromMinutes(1));
        }

        private ScheduledTask CalculateNextInvocation(ScheduledTask job)
        {
            var schedule = CrontabSchedule.Parse(job.Cron);
            job.NextInvocation = schedule.GetNextOccurrence(DateTime.UtcNow);
            return job;
        }
    }
}
