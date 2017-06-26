using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Server
{
    internal class AutomaticRetryProcess : IBackgroundProcessWrapper
    {
        private static readonly TimeSpan DefaultMaxAttemptDelay = TimeSpan.FromMinutes(5);
        private const int DefaultMaxRetryAttempts = int.MaxValue;

        private readonly IBackgroundProcess _innerProcess;

        public AutomaticRetryProcess(IBackgroundProcess innerProcess)
        {
            if (innerProcess == null) throw new ArgumentNullException(nameof(innerProcess));

            _innerProcess = innerProcess;
            //_logger = LogProvider.GetLogger(_innerProcess.GetProcessType());

            MaxRetryAttempts = DefaultMaxRetryAttempts;
            MaxAttemptDelay = DefaultMaxAttemptDelay;
            DelayCallback = GetBackOffMultiplier;
        }

        public int MaxRetryAttempts { get; set; }
        public TimeSpan MaxAttemptDelay { get; set; }
        public Func<int, TimeSpan> DelayCallback { get; set; }

        public IBackgroundProcess InnerProcess => _innerProcess;

        public void Execute(BackgroundProcessContext context)
        {
            for (var i = 0; i <= MaxRetryAttempts; i++)
            {
                try
                {
                    _innerProcess.Execute(context);
                    return;
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException && context.IsShutdownRequested)
                    {
                        throw;
                    }

                    // Break the loop after the retry attempts number exceeded.
                    if (i >= MaxRetryAttempts - 1) throw;

                    var nextTry = DelayCallback(i);
                    //var logLevel = GetLogLevel(i);

                    //_logger.Log(
                    //    logLevel,
                    //    // ReSharper disable once AccessToModifiedClosure
                    //    () => $"Error occurred during execution of '{_innerProcess}' process. Execution will be retried (attempt #{i + 1}) in {nextTry} seconds.",
                    //    ex);

                    context.Wait(nextTry);

                    if (context.IsShutdownRequested)
                    {
                        break;
                    }
                }
            }
        }

        //private static LogLevel GetLogLevel(int i)
        //{
        //    switch (i)
        //    {
        //        case 0:
        //            return LogLevel.Debug;
        //        case 1:
        //            return LogLevel.Info;
        //        case 2:
        //            return LogLevel.Warn;
        //    }

        //    return LogLevel.Error;
        //}

        public override string ToString()
        {
            return _innerProcess.ToString();
        }

        private TimeSpan GetBackOffMultiplier(int retryAttemptNumber)
        {
            //exponential/random retry back-off.
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var nextTry = rand.Next(
                (int)Math.Pow(retryAttemptNumber, 2), (int)Math.Pow(retryAttemptNumber + 1, 2) + 1);

            return TimeSpan.FromSeconds(Math.Min(nextTry, MaxAttemptDelay.TotalSeconds));
        }
    }
}
