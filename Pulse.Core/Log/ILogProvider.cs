using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Log
{
    public interface ILogProvider
    {
        ILog GetLogger();
    }
}
