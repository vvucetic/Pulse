using Pulse.Core.Common;
using Pulse.Core.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Common
{
    public class WorkflowJob
    {
        public Guid TempId { get; set; } = Guid.NewGuid();
        public QueueJob QueueJob { get; set; }
        public List<WorkflowJob> NextJobs { get; set; } = new List<WorkflowJob>();
        public static WorkflowJob MakeJob(Expression<Action> methodCall, string queue = EnqueuedState.DefaultQueue, Guid? contextId = null, int maxRetries = 10, string description = null)
        {
            return new WorkflowJob()
            {
                QueueJob = new QueueJob()
                {
                    Job = Job.FromExpression(methodCall),
                    QueueName = queue,
                    ContextId = contextId,
                    MaxRetries = maxRetries,
                    Description = description
                }
            };
        }

        public WorkflowJob ContinueWith(WorkflowJob workflowJob)
        {
            if (this.TempId == workflowJob.TempId)
                throw new Exception("Job cannot continue with it self!");
            this.NextJobs.Add(workflowJob);
            return this;
        }

        //public WorkflowJobGroup ContinueWithGroup(params WorkflowJob[] workflowJobs)
        //{
        //    foreach (var job in workflowJobs)
        //    {
        //        this.NextJobs.Add(job);
        //    }
        //    return new WorkflowJobGroup() { Jobs = workflowJobs.ToList() };
        //}

        public WorkflowJob ContinueWithGroup(WorkflowJobGroup workflowJobGroup)
        {
            foreach (var job in workflowJobGroup.Jobs)
            {
                if (this.TempId == job.TempId)
                    throw new Exception("Job cannot continue with it self!");
                this.NextJobs.Add(job);
            }
            return this;
        }

        public WorkflowJob WaitFor(params WorkflowJob[] jobs)
        {
            foreach (var job in jobs)
            {
                if (this.TempId == job.TempId)
                    throw new Exception("Job cannot wait for its self!");
                job.NextJobs.Add(this);
            }
            return this;
        }

        public override string ToString() => $"TempID: {TempId}, JobId: {QueueJob.JobId}, NextJobs: {string.Join(",", NextJobs.Select(t=>t.QueueJob.JobId))}";
    }
}
