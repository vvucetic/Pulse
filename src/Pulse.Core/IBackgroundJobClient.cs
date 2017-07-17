using Pulse.Core.Common;
using System;

namespace Pulse.Core
{
    public interface IBackgroundJobClient
    {
        int CreateAndEnqueue(Job job, string queue, int maxRetries = 10, Guid? contextId = null);

        void CreateAndEnqueue(Workflow workflow);
    }
}