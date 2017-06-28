using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    public interface IBackgroundProcess 
    {
        void Execute(BackgroundProcessContext context);
    }
}
