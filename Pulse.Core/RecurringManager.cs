using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulse.Core.Common;
using Pulse.Core.Storage;
using NCrontab;

namespace Pulse.Core
{
    public class RecurringManager : IRecurringManager
    {
        private readonly DataStorage _storage;

        public RecurringManager() : this(DataStorage.Current)
        {
        }

        public RecurringManager(DataStorage storage)
        {
            this._storage = storage;
        }

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            if (options == null) throw new ArgumentNullException(nameof(options));

            ValidateCronExpression(cronExpression);

            _storage.CreateOrUpdateRecurringTask(new ScheduledTask()
            {
                Cron = cronExpression,
                LastInvocation = DateTime.UtcNow,
                Name = recurringJobId,
                NextInvocation = CrontabSchedule.Parse(cronExpression).GetNextOccurrence(DateTime.UtcNow),
                Job = new QueueJob()
                {
                    Job = job,
                    QueueName = options.QueueName,
                    MaxRetries = options.MaxRetries,
                    ContextId = options.ContextId
                }
            });
        }

        public void AddOrUpdate(string recurringJobId, Workflow workflow, string cronExpression)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));

            ValidateCronExpression(cronExpression);
            _storage.CreateOrUpdateRecurringTask(new ScheduledTask()
            {
                Cron = cronExpression,
                LastInvocation = DateTime.UtcNow,
                Name = recurringJobId,
                NextInvocation = CrontabSchedule.Parse(cronExpression).GetNextOccurrence(DateTime.UtcNow),
                Workflow = workflow
            });
        }

        public void RemoveIfExists(string recurringJobId)
        {
            //TODO RemoveIfExists
            throw new NotImplementedException();
        }

        public void Trigger(string recurringJobId)
        {
            //TODO Trigger
            throw new NotImplementedException();
        }

        private static void ValidateCronExpression(string cronExpression)
        {
            try
            {
                var schedule = CrontabSchedule.Parse(cronExpression);
                schedule.GetNextOccurrence(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("CRON expression is invalid. Please see the inner exception for details.", nameof(cronExpression), ex);
            }
        }

    }
}
