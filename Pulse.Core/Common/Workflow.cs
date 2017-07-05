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
        private readonly string _name;
        private readonly Guid? _contextId;
        private readonly WorkflowJob _rootWorkflowJob;
        private readonly WorkflowJobGroup _rootWorkflowJobGroup;


        public Workflow(WorkflowJob workflowJob, Guid? contextId = null)
        {
            this._rootWorkflowJob = workflowJob;
            if (contextId != null)
            {
                GetAllJobs().ForEach(t => t.QueueJob.ContextId = contextId);
            }
        }

        public Workflow(WorkflowJobGroup wfGroup, Guid? contextId = null)
        {
            this._rootWorkflowJobGroup = wfGroup;
            if (contextId != null)
            {
                GetAllJobs().ForEach(t => t.QueueJob.ContextId = contextId);
            }
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
                var savedIds = new HashSet<Guid>();
                SaveRecursiely(this._rootWorkflowJob, saveJob, savedIds);
                saveJob(this._rootWorkflowJob);
            }
        }

        private void SaveRecursiely(WorkflowJob wfJob, Func<WorkflowJob, int> saveJob, HashSet<Guid> savedIds)
        {
            if (wfJob.NextJobs.Any())
            {
                wfJob.QueueJob.NextJobs = SaveJobs(wfJob.NextJobs, saveJob, savedIds).ToList();
            }
            foreach (var job in wfJob.NextJobs)
            {
                SaveRecursiely(job, saveJob, savedIds);
            }
        }

        private IEnumerable<int> SaveJobs(IEnumerable<WorkflowJob> jobs, Func<WorkflowJob, int> saveJob, HashSet<Guid> savedIds)
        {
            foreach (var job in jobs.Where(t => !savedIds.Contains(t.TempId)))
            {
                savedIds.Add(job.TempId);
                yield return saveJob(job);
            }
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(_rootWorkflowJob, Formatting.None, new JobConverter());
        }

    }
}
