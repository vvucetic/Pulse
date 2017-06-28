using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public static class BackgroundJobClientExtensions
    {
        public static int Enqueue(this IBackgroundJobClient client, Expression<Action> methodCall, string queue = "default")
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.CreateAndEnqueue(Job.FromExpression(methodCall), queue);
        }

        public static int Enqueue(this IBackgroundJobClient client, Expression<Func<Task>> methodCall, string queue = "default")
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.CreateAndEnqueue(Job.FromExpression(methodCall), queue);
        }

        public static int Enqueue<T>(this IBackgroundJobClient client, Expression<Action<T>> methodCall, string queue = "default")
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.CreateAndEnqueue(Job.FromExpression(methodCall), queue);
        }
        
        public static int Enqueue<T>(this IBackgroundJobClient client, Expression<Func<T, Task>> methodCall, string queue = "default")
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.CreateAndEnqueue(Job.FromExpression(methodCall), queue);
        }
    }
}
