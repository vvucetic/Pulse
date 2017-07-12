using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.States
{
    public class ConsequentlyFailed : IState
    {
        public ConsequentlyFailed(string reason, int failedParentId)
        {
            this.Reason = reason;
            this.FailedAt = DateTime.UtcNow;
            this.FailedParentId = failedParentId;
        }

        public static string DefaultName = "ConsequentlyFailed";
        public string Name => ConsequentlyFailed.DefaultName;

        public string Reason { get; set; }

        public bool IsFinal => false;

        public DateTime FailedAt { get; }

        public int FailedParentId { get; set; }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "FailedAt", JobHelper.SerializeDateTime(FailedAt) },
                { "FailedParentId", this.FailedParentId.ToString() }
            };
        }
    }
}
