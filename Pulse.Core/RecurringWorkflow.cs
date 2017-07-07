using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public static class RecurringWorkflow
    {
        private static readonly Lazy<RecurringManager> Instance = new Lazy<RecurringManager>(
                    () => new RecurringManager());

        public static void AddOrUpdate(string recurringJobId, Workflow workflow, string cron)
        {
            Instance.Value.AddOrUpdate(recurringJobId, workflow, cron);
        }
    }
}
