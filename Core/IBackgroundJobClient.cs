using Core.Common;

namespace Core
{
    public interface IBackgroundJobClient
    {
        int CreateAndEnqueue(Job job, string queue);
    }
}