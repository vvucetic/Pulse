using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.States
{
    public class EnqueuedState : IState
    {
        public const string DefaultQueue = "default";
        public static string DefaultName = "Enqueued";
        public string Name => EnqueuedState.DefaultName;

        public string Reason { get; set; }

        public bool IsFinal => false;

        public DateTime EnqueuedAt { get; set; }

        public string Queue { get; set; } = DefaultQueue;

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "EnqueuedAt", JobHelper.SerializeDateTime(EnqueuedAt) },
                { "Queue", Queue }
            };
        }
    }
}
