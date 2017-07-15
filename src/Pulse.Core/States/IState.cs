using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.States
{
    public interface IState
    {
        string Name { get; }

        string Reason { get; }

        bool IsFinal { get; }
        
        //bool IgnoreJobLoadException { get; }
        
        Dictionary<string, string> SerializeData();
    }
}
