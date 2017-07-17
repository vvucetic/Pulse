using Pulse.Core.Common;
using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public class BackgroundJobClient : IBackgroundJobClient
    {
        private readonly DataStorage _storage;

        public BackgroundJobClient() : this(DataStorage.Current)
        {

        }

        public BackgroundJobClient(DataStorage storage)
        {
            this._storage = storage;
        }

        public int CreateAndEnqueue(Job job, string queue = "default", int maxRetries = 10, Guid? contextId = null)
        {
            if(job == null) throw new ArgumentException("Job must not be empty", nameof(job));
            if (string.IsNullOrEmpty(queue)) throw new ArgumentException("Queue must not be empty", nameof(queue));
            return _storage.CreateAndEnqueueJob(new QueueJob()
            {
                ContextId = contextId,
                Job = job,
                QueueName = queue,
                MaxRetries = maxRetries
            });
        }

        public void CreateAndEnqueue(Workflow workflow)
        {
            if (workflow == null) throw new ArgumentException("Workflow must not be empty", nameof(workflow));
            _storage.CreateAndEnqueueWorkflow(workflow);
        }
    }
}
