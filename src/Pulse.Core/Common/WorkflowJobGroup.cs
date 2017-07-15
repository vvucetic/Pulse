using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Common
{
    public class WorkflowJobGroup
    {
        public List<WorkflowJob> Jobs = new List<WorkflowJob>();
        public static WorkflowJobGroup RunInParallel(params WorkflowJob[] jobs)
        {
            return new WorkflowJobGroup()
            {
                Jobs = jobs.ToList()
            };
        }

        public WorkflowJobGroup ContinueWith(WorkflowJob workflowJob)
        {
            foreach (var job in this.Jobs)
            {
                job.NextJobs.Add(workflowJob);
            }
            return this;
        }
    }
}
