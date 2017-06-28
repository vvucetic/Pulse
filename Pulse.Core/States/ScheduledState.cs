using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.States
{
    public class ScheduledState : IState
    {
        public string Name => "Scheduled";

        public string Reason { get; set; }

        public bool IsFinal => false;

        public DateTime EnqueueAt { get; }
       
        public DateTime ScheduledAt { get; }

        public ScheduledState(TimeSpan enqueueIn)
    : this(DateTime.UtcNow.Add(enqueueIn))
        {
        }
        
        public ScheduledState(DateTime enqueueAt)
        {
            EnqueueAt = enqueueAt;
            ScheduledAt = DateTime.UtcNow;
        }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "EnqueueAt", JobHelper.SerializeDateTime(EnqueueAt) },
                { "ScheduledAt", JobHelper.SerializeDateTime(ScheduledAt) }
            };
        }
    }
}
