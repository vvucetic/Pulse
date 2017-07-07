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

        public readonly WorkflowJob RootWorkflowJob;

        public readonly WorkflowJobGroup RootWorkflowJobGroup;


        public Workflow(WorkflowJob workflowJob, Guid? contextId = null)
        {
            this.ContextId = contextId;
            this.RootWorkflowJob = workflowJob;

            GetAllJobs().ForEach(t =>
            {
                t.QueueJob.ContextId = this.ContextId;
                t.QueueJob.WorkflowId = this.WorkflowId;
            });
        }

        public Workflow(WorkflowJobGroup wfGroup, Guid? contextId = null)
        {
            this.ContextId = contextId;
            this.RootWorkflowJobGroup = wfGroup;

            GetAllJobs().ForEach(t => 
            {
                t.QueueJob.ContextId = this.ContextId;
                t.QueueJob.WorkflowId = this.WorkflowId;
            });            
        }

        [JsonConstructor]
        public Workflow(WorkflowJob rootWorkflowJob, WorkflowJobGroup rootWorkflowJobGroup)
        {
            if(rootWorkflowJob!=null)
            {
                this.RootWorkflowJob = rootWorkflowJob;
            }
            else if(rootWorkflowJobGroup!=null)
            {
                this.RootWorkflowJobGroup = rootWorkflowJobGroup;
            }
            else
            {
                throw new Exception("Root Workflow job or Workflow group not set!");
            }
            GetAllJobs().ForEach(t =>
            {
                t.QueueJob.ContextId = this.ContextId;
                t.QueueJob.WorkflowId = this.WorkflowId;
            });
        }

        public List<WorkflowJob> GetRootJobs()
        {
            if (this.RootWorkflowJob != null)
            {
                return new List<WorkflowJob>() { this.RootWorkflowJob };
            }
            else if (this.RootWorkflowJobGroup != null)
            {
                return this.RootWorkflowJobGroup.Jobs;
            }
            else
            {
                throw new Exception("Root Workflow job or Workflow group not set!");
            }
        }

        public List<WorkflowJob> GetAllJobs()
        {
            if (this.RootWorkflowJob != null)
            {
                var enumeratedIds = new HashSet<Guid>();
                return GetJobsRecursively(this.RootWorkflowJob, enumeratedIds).ToList();
            }
            else if (this.RootWorkflowJobGroup!=null)
            {
                var list = new List<WorkflowJob>();
                var enumeratedIds = new HashSet<Guid>();
                this.RootWorkflowJobGroup.Jobs.ForEach(t => list.AddRange( GetJobsRecursively(t, enumeratedIds)));
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
            if(this.RootWorkflowJob != null)
            {
                var savedIds = new Dictionary<Guid, int>();
                SaveRecursiely(this.RootWorkflowJob, saveJob, savedIds);
                var jobId = saveJob(this.RootWorkflowJob);
                this.RootWorkflowJob.QueueJob.JobId = jobId;
            }
            else if(this.RootWorkflowJobGroup != null)
            {
                var savedIds = new Dictionary<Guid, int>();
                foreach (var job in this.RootWorkflowJobGroup.Jobs)
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
