using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Log
{
    public class ConsoleLogger : ILog
    {
        public void Debug(Exception ex, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{message} : {ex}");
        }

        public void Debug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"{message}");
        }

        public void Error(Exception ex, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{message} : {ex}");
        }

        public void Fatal(Exception ex, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{message} : {ex}");
        }

        public void Information(string message)
        {
            System.Diagnostics.Debug.WriteLine($"{message}");
        }

        public void Log(LogLevel level, string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{level.ToString().ToUpper()} - {message} : {ex}");
        }

        public void Log(LogLevel level, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{level.ToString().ToUpper()} - {message}");
        }
        
        public void Warning(Exception ex, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{message} : {ex}");
        }

        public void Log(string message)
        {
            Log(LogLevel.Information, message);
        }
    }
}
