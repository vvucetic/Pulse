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
    public class RecurringJobManager : IRecurringJobManager
    {
        private readonly DataStorage _storage;

        public RecurringJobManager() : this(DataStorage.Current)
        {
        }

        public RecurringJobManager(DataStorage storage)
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

            _storage.CreateOrUpdateRecurringJob(new ScheduledJob()
            {
                Cron = cronExpression,
                LastInvocation = DateTime.UtcNow,
                Name = recurringJobId,
                NextInvocation = CrontabSchedule.Parse(cronExpression).GetNextOccurrence(DateTime.UtcNow),
                QueueJob = new QueueJob()
                {
                    Job = job,
                    QueueName = options.QueueName,
                    MaxRetries = options.MaxRetries,
                    ContextId = options.ContextId
                }
            });

        }

        public void RemoveIfExists(string recurringJobId)
        {
            throw new NotImplementedException();
        }

        public void Trigger(string recurringJobId)
        {
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
