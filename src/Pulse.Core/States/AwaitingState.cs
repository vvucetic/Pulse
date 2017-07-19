using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.States
{
    public class AwaitingState : IState
    {
        public const string DefaultName = "Awaiting";
        public string Name => AwaitingState.DefaultName;

        public string Reason { get; set; }

        public bool IsFinal => false;

        public DateTime CreatedAt { get; set; }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "CreatedAt",  JobHelper.SerializeDateTime(CreatedAt) }
            };            
        }
    }
}
