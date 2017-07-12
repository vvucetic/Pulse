using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.States
{
    public class ProcessingState : IState
    {
        public static string DefaultName = "Processing";
        public string Name => ProcessingState.DefaultName;

        public string Reason { get; set; }

        public bool IsFinal => false;

        public DateTime StartedAt { get; }

        public string ServerId { get; }

        public string WorkerId { get; }

        public ProcessingState(string serverId, string workerId)
        {
            if (String.IsNullOrWhiteSpace(serverId)) throw new ArgumentNullException(nameof(serverId));
            if (String.IsNullOrWhiteSpace(workerId)) throw new ArgumentNullException(nameof(workerId));

            ServerId = serverId;
            StartedAt = DateTime.UtcNow;
            WorkerId = workerId;
        }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "StartedAt", JobHelper.SerializeDateTime(StartedAt) },
                { "ServerId", ServerId },
                { "WorkerId", WorkerId }
            };
        }
    }
}
