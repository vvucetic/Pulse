﻿using Pulse.Core.Common;

namespace Pulse.Core
{
    public interface IBackgroundJobClient
    {
        int CreateAndEnqueue(Job job, string queue, int maxRetries = 10);

        void CreateAndEnqueue(Workflow workflow);
    }
}