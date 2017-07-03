using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public interface IRecurringJobManager
    {
        void AddOrUpdate(
            string recurringJobId,
            Job job,
            string cronExpression,
            RecurringJobOptions options);

        void Trigger(string recurringJobId);
        void RemoveIfExists(string recurringJobId);
    }
}
