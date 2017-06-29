using Pulse.Core.Common;
using Pulse.Core.Exceptions;
using Pulse.Core.States;
using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server.Processes
{
    public class WorkerProcess : IBackgroundProcess
    {
        private readonly string _workerId;

        private readonly string[] _queues;

        private readonly IBackgroundJobPerformer _performer;

        private readonly DataStorage _storage;

        public WorkerProcess(string[] queues, IBackgroundJobPerformer performer, DataStorage storage)
        {
            this._workerId = Guid.NewGuid().ToString();
            this._queues = queues ?? throw new ArgumentNullException(nameof(queues));
            this._performer = performer ?? throw new ArgumentNullException(nameof(performer));
            this._storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void Execute(BackgroundProcessContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var queueJob = _storage.FetchNextJob(_queues);
            if(queueJob==null)
            {
                context.Wait(TimeSpan.FromSeconds(5));
                return;
            }
            _storage.InsertAndSetJobState(queueJob.JobId, new ProcessingState(context.ServerId, this._workerId));
            var perfomContext = new PerformContext(context.CancellationToken, queueJob);
            var resultState = PerformJob(perfomContext);
            if(resultState is FailedState)
            {
                var failedState = resultState as FailedState;
                if (queueJob.RetryCount < queueJob.MaxRetries)
                {
                    var nextRun = DateTime.UtcNow.AddSeconds(SecondsToDelay(queueJob.RetryCount));
                    
                    const int maxMessageLength = 50;
                    var exceptionMessage = failedState.Exception.Message.Length > maxMessageLength
                        ? failedState.Exception.Message.Substring(0, maxMessageLength - 1) + "…"
                        : failedState.Exception.Message;
                    var scheduledState = new ScheduledState(nextRun)
                    {
                        Reason = $"Retry attempt { queueJob.RetryCount } of { queueJob.MaxRetries }: { exceptionMessage}"
                    };
                    _storage.UpgradeStateToScheduled(queueJob.JobId, resultState, scheduledState, nextRun, queueJob.RetryCount + 1);
                }
                else
                {
                    _storage.InsertAndSetJobState(queueJob.JobId, resultState);
                }
            }
            else
            {
                //Succeeded
                _storage.InsertAndSetJobState(queueJob.JobId, resultState);
            }
            _storage.RemoveFromQueue(queueJob.QueueJobId);
        }
        private static int SecondsToDelay(long retryCount)
        {
            var random = new Random();
            return (int)Math.Round(
                Math.Pow(retryCount - 1, 4) + 15 + random.Next(30) * retryCount);
        }

        private IState PerformJob(PerformContext performContext)
        {
            try
            {
                var latency = (DateTime.UtcNow - performContext.QueueJob.CreatedAt).TotalMilliseconds;
                var duration = Stopwatch.StartNew();

                var result = this._performer.Perform(performContext);

                return new SucceededState(result, (long)latency, duration.ElapsedMilliseconds);

            }
            catch (JobPerformanceException ex)
            {
                return new FailedState(ex.InnerException)
                {
                    Reason = ex.Message
                };
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException && performContext.CancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                return new FailedState(ex)
                {
                    Reason = "An exception occurred during processing of a background job."
                };
            }        
        }
        
        public override string ToString()
        {
            return $"{GetType().Name} #{_workerId.Substring(0, 8)}";
        }
    }
}
