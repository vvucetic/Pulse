using Core.Common;
using Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class BackgroundJobClient : IBackgroundJobClient
    {
        private readonly DataStorage _storage;

        public BackgroundJobClient(DataStorage storage)
        {
            this._storage = storage;
        }

        public int CreateAndEnqueue(Job job, string queue = "default")
        {
            if (string.IsNullOrEmpty(queue)) throw new ArgumentException("Queue must not be empty", nameof(queue));
            return _storage.CreateAndEnqueue(new QueueJob()
            {
                ContextId = null,
                Job = job,
                NumberOfConditionJobs = 0,
                QueueName = queue
            });
        }
    }
}
