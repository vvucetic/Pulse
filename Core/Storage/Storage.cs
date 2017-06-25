using Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Storage
{
    public abstract class Storage
    {
        public virtual void Dispose()
        {
        }

        //public abstract void FetchNextJob(string[] queues, CancellationToken cancellationToken);

        //public abstract void GetJobData(string jobId);

        public abstract QueueJob FetchNextJob(string queue);
    }
}
