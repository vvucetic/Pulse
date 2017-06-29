using Pulse.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server.Processes
{
    internal class AutomaticRetryProcess : IBackgroundProcessWrapper
    {
        private static readonly TimeSpan DefaultMaxAttemptDelay = TimeSpan.FromMinutes(5);
        private const int DefaultMaxRetryAttempts = int.MaxValue;
        private readonly ILog _logger = LogProvider.GetLogger();
        private readonly IBackgroundProcess _innerProcess;

        public AutomaticRetryProcess(IBackgroundProcess innerProcess)
        {
            _innerProcess = innerProcess ?? throw new ArgumentNullException(nameof(innerProcess));

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
                    if (i >= MaxRetryAttempts - 1)
                    {
                        _logger.Log(LogLevel.Error, $"Error occurred during execution of '{_innerProcess}' process. No more retries. Exiting background process.", ex);

                        throw;
                    }

                    var nextTry = DelayCallback(i);
                    _logger.Log(LogLevel.Warning, $"Error occurred during execution of '{_innerProcess}' process. Execution will be retried (attempt #{i + 1}) in {nextTry} seconds.", ex);

                    context.Wait(nextTry);

                    if (context.IsShutdownRequested)
                    {
                        break;
                    }
                }
            }
        }
        
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
