using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Events
{
    public static class EventSubscriberProvider
    {
        private static EventSubscriber _currentSubscriber = new EventSubscriber();

        public static void SetCurrentSubscriber(EventSubscriber subscriber)
        {
            _currentSubscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public static EventSubscriber GetSubscriber()
        {
            return _currentSubscriber;
        }
    }
}
