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

        public void AddOrUpdate(string recurringJobName, Job job, string cronExpression, RecurringJobOptions options)
        {
            if (recurringJobName == null) throw new ArgumentNullException(nameof(recurringJobName));
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            if (options == null) throw new ArgumentNullException(nameof(options));

            ValidateCronExpression(cronExpression);

            _storage.CreateOrUpdateRecurringTask(new ScheduledTask()
            {
                Cron = cronExpression,
                LastInvocation = DateTime.UtcNow,
                Name = recurringJobName,
                NextInvocation = CrontabSchedule.Parse(cronExpression).GetNextOccurrence(DateTime.UtcNow),
                Job = new QueueJob()
                {
                    Job = job,
                    QueueName = options.QueueName,
                    MaxRetries = options.MaxRetries,
                    ContextId = options.ContextId,
                    Description = options.Description
                },
                OnlyIfLastFinishedOrFailed = options.OnlyIfLastFinishedOrFailed
            });
        }

        public void AddOrUpdate(string recurringJobName, Workflow workflow, string cronExpression, bool onlyIfLastFinishedOrFailed = false)
        {
            if (recurringJobName == null) throw new ArgumentNullException(nameof(recurringJobName));
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));

            ValidateCronExpression(cronExpression);
            _storage.CreateOrUpdateRecurringTask(new ScheduledTask()
            {
                Cron = cronExpression,
                LastInvocation = DateTime.UtcNow,
                Name = recurringJobName,
                NextInvocation = CrontabSchedule.Parse(cronExpression).GetNextOccurrence(DateTime.UtcNow),
                Workflow = workflow,
                OnlyIfLastFinishedOrFailed = onlyIfLastFinishedOrFailed
            });
        }

        public void RemoveIfExists(string recurringJobName)
        {
            _storage.RemoveScheduledItem(recurringJobName);
        }

        public void Trigger(string recurringJobName)
        {
            _storage.TriggerScheduledJob(recurringJobName);
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
