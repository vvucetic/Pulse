using Pulse.Core.Common;

namespace Pulse.Core
{
    public interface IBackgroundJobClient
    {
        int CreateAndEnqueue(Job job, string queue);
    }
}