using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server.Processes
{
    internal class InfiniteLoopProcess : IBackgroundProcessWrapper
    {
        public InfiniteLoopProcess(IBackgroundProcess innerProcess)
        {
            if (innerProcess == null) throw new ArgumentNullException(nameof(innerProcess));
            InnerProcess = innerProcess;
        }

        public IBackgroundProcess InnerProcess { get; }

        public void Execute(BackgroundProcessContext context)
        {
            while (!context.IsShutdownRequested)
            {
                InnerProcess.Execute(context);
            }
        }

        public override string ToString()
        {
            return InnerProcess.ToString();
        }
    }
}
