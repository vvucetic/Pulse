using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Log
{
    public interface ILog
    {
        void Debug(Exception ex, string message);
        void Debug(string message);
        void Error(Exception ex, string message);
        void Warning(Exception ex, string message);
        void Fatal(Exception ex, string message);
        void Information(string message);
        void Log(LogLevel level, string message, Exception ex);
        void Log(LogLevel level, string message);
        void Log(string message);
    }

    public enum LogLevel
    {
        Debug = 0,
        Information = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }
}
