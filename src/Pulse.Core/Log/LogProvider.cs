using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Log
{
    public static class LogProvider
    {
        private static ILog _currentLogger = new ConsoleLogger();

        public static void SetCurrentLogger(ILog logger)
        {
            _currentLogger = logger;
        }

        public static ILog GetLogger()
        {
            return _currentLogger;
        }
    }
}
