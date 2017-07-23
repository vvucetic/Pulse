using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Log
{
    public class ConsoleLogger : ILog
    {
        public void Log(LogLevel level, string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{level.ToString().ToUpper()} - {message} : {ex}");
        }

        public void Log(LogLevel level, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{level.ToString().ToUpper()} - {message}");
        }
        
        public void Log(string message)
        {
            Log(LogLevel.Information, message);
        }
    }
}
