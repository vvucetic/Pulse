using Pulse.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    internal static class ServerProcessExtensions
    {
        public static void Execute(this IBackgroundProcess process, BackgroundProcessContext context)
        {
            process.Execute(context);            
        }

        public static Task CreateTask(this IBackgroundProcess process, BackgroundProcessContext context)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            if (!(process is IBackgroundProcess))
            {
                throw new ArgumentOutOfRangeException(nameof(process), "Long-running process must be of type IServerComponent or IBackgroundProcess.");
            }

            return Task.Factory.StartNew(
                () => RunProcess(process, context),
                TaskCreationOptions.LongRunning);
        }

        public static Type GetProcessType(this IBackgroundProcess process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            var nextProcess = process;

            while (nextProcess is IBackgroundProcessWrapper)
            {
                nextProcess = ((IBackgroundProcessWrapper)nextProcess).InnerProcess;
            }

            return nextProcess.GetType();
        }

        private static void RunProcess(IBackgroundProcess process, BackgroundProcessContext context)
        {
            // Long-running tasks are based on custom threads (not threadpool ones) as in 
            // .NET Framework 4.5, so we can try to set custom thread name to simplify the
            // debugging experience.
            TrySetThreadName(process.ToString());

            // LogProvider.GetLogger does not throw any exception, that is why we are not
            // using the `try` statement here. It does not return `null` value as well.
            var logger = LogProvider.GetLogger();
            
            logger.Log(LogLevel.Debug,$"Background process '{process}' started.");

            try
            {
                process.Execute(context);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException && context.IsShutdownRequested)
                {
                    // Graceful shutdown
                    logger.Log(LogLevel.Debug, $"Background process '{process}' was stopped due to a shutdown request.");
                    
                }
                else
                {
                    logger.Log(LogLevel.Fatal, $"Fatal error occurred during execution of '{process}' process. It will be stopped. See the exception for details.", ex);
                }
            }

            logger.Log(LogLevel.Debug, $"Background process '{process}' stopped.");
        }

        private static void TrySetThreadName(string name)
        {
            try
            {
                Thread.CurrentThread.Name = name;
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
