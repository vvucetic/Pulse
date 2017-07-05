using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.States
{
    public class AwaitingState : IState
    {
        public string Name => "Awaiting";

        public string Reason { get; set; }

        public bool IsFinal => false;

        public Dictionary<string, string> SerializeData()
        {
            throw new NotImplementedException();
        }
    }
}
