using Pulse.Core.Common;
using Pulse.Core.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public static class RecurringJobManagerExtensions
    {
        public static void AddOrUpdate(
            this IRecurringManager manager,
            string recurringJobId,
            Job job,
            string cronExpression,
            bool onlyIfLastFinishedOrFailed)
        {
            AddOrUpdate(manager, recurringJobId, job, cronExpression, TimeZoneInfo.Utc, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate(
            this IRecurringManager manager,
            string recurringJobId,
            Job job,
            string cronExpression,
            TimeZoneInfo timeZone,
            bool onlyIfLastFinishedOrFailed)
        {
            AddOrUpdate(manager, recurringJobId, job, cronExpression, timeZone, EnqueuedState.DefaultQueue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate(
            this IRecurringManager manager,
            string recurringJobId,
            Job job,
            string cronExpression,
            TimeZoneInfo timeZone,
            string queue,
            bool onlyIfLastFinishedOrFailed)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (timeZone == null) throw new ArgumentNullException(nameof(timeZone));
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            manager.AddOrUpdate(
                recurringJobId,
                job,
                cronExpression,
                new RecurringJobOptions { QueueName = queue, TimeZone = timeZone, OnlyIfLastFinishedOrFailed = onlyIfLastFinishedOrFailed });
        }
    }
}
