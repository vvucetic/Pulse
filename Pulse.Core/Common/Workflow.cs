using Newtonsoft.Json;
using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Common
{
    public class Workflow
    {
        public string Name { get; set; }

        public Guid? ContextId { get; set; }

        public Guid WorkflowId { get; } = Guid.NewGuid();

        private readonly WorkflowJob _rootWorkflowJob;

        private readonly WorkflowJobGroup _rootWorkflowJobGroup;


        public Workflow(WorkflowJob workflowJob, Guid? contextId = null)
        {
            this.ContextId = contextId;
            this._rootWorkflowJob = workflowJob;

            GetAllJobs().ForEach(t =>
            {
                t.QueueJob.ContextId = this.ContextId;
                t.QueueJob.WorkflowId = this.WorkflowId;
            });
        }

        public Workflow(WorkflowJobGroup wfGroup, Guid? contextId = null)
        {
            this.ContextId = contextId;
            this._rootWorkflowJobGroup = wfGroup;

            GetAllJobs().ForEach(t => 
            {
                t.QueueJob.ContextId = this.ContextId;
                t.QueueJob.WorkflowId = this.WorkflowId;
            });            
        }

        public List<WorkflowJob> GetRootJobs()
        {
            if (this._rootWorkflowJob != null)
            {
                return new List<WorkflowJob>() { this._rootWorkflowJob };
            }
            else if (this._rootWorkflowJobGroup != null)
            {
                return this._rootWorkflowJobGroup.Jobs;
            }
            else
            {
                throw new Exception("Root Workflow job or Workflow group not set!");
            }
        }

        public List<WorkflowJob> GetAllJobs()
        {
            if (this._rootWorkflowJob != null)
            {
                var enumeratedIds = new HashSet<Guid>();
                return GetJobsRecursively(this._rootWorkflowJob, enumeratedIds).ToList();
            }
            else if (this._rootWorkflowJobGroup!=null)
            {
                var list = new List<WorkflowJob>();
                var enumeratedIds = new HashSet<Guid>();
                this._rootWorkflowJobGroup.Jobs.ForEach(t => list.AddRange( GetJobsRecursively(t, enumeratedIds)));
                return list;
            }
            else
            {
                throw new Exception("Root Workflow job or Workflow group not set!");
            }
        }

        private IEnumerable<WorkflowJob> GetJobsRecursively(WorkflowJob wf, HashSet<Guid> enumeratedIds)
        {
            if(wf.NextJobs.Any())
            {
                foreach (var item in wf.NextJobs.Where(t => !enumeratedIds.Contains(t.TempId)))
                {
                    foreach (var resultedJob in GetJobsRecursively(item, enumeratedIds).ToList())
                    {
                        enumeratedIds.Add(resultedJob.TempId);
                        yield return resultedJob;
                    }

                }
            }
            enumeratedIds.Add(wf.TempId);
            yield return wf;
        }

        public void SaveWorkflow(Func<WorkflowJob, int> saveJob)
        {
            if(this._rootWorkflowJob != null)
            {
                var savedIds = new Dictionary<Guid, int>();
                SaveRecursiely(this._rootWorkflowJob, saveJob, savedIds);
                var jobId = saveJob(this._rootWorkflowJob);
                this._rootWorkflowJob.QueueJob.JobId = jobId;
            }
            else if(this._rootWorkflowJobGroup != null)
            {
                var savedIds = new Dictionary<Guid, int>();
                foreach (var job in this._rootWorkflowJobGroup.Jobs)
                {
                    SaveRecursiely(job, saveJob, savedIds);
                    var jobId = saveJob(job);
                    job.QueueJob.JobId = jobId;
                }
            }
        }

        private void SaveRecursiely(WorkflowJob wfJob, Func<WorkflowJob, int> saveJob, Dictionary<Guid, int> savedIds)
        {
            foreach (var job in wfJob.NextJobs)
            {
                SaveRecursiely(job, saveJob, savedIds);

            }
            wfJob.QueueJob.NextJobs = SaveJobs(wfJob.NextJobs, saveJob, savedIds).ToList();

        }

        private IEnumerable<int> SaveJobs(IEnumerable<WorkflowJob> jobs, Func<WorkflowJob, int> saveJob, Dictionary<Guid, int> savedIds)
        {
            foreach (var job in jobs)
            {
                if (savedIds.ContainsKey(job.TempId))
                    yield return savedIds[job.TempId];
                else
                {
                    var jobId = saveJob(job);
                    savedIds.Add(job.TempId, jobId);
                    job.QueueJob.JobId = jobId;
                    yield return jobId;
                }
            }
        }
        
    }
}
