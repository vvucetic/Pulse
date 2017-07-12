using Pulse.Core.Common;
using Pulse.Core.Exceptions;
using Pulse.Core.States;
using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.Core.Server.Processes
{
    public class WorkerProcess : IBackgroundProcess
    {
        private readonly string _workerId;

        private readonly string _serverId;

        private readonly string[] _queues;

        private readonly IBackgroundJobPerformer _performer;

        private readonly DataStorage _storage;
        
        public WorkerProcess(string[] queues, IBackgroundJobPerformer performer, DataStorage storage, string serverId)
        {
            this._workerId = Guid.NewGuid().ToString();
            this._serverId = serverId ?? throw new ArgumentNullException(nameof(serverId));
            this._queues = queues ?? throw new ArgumentNullException(nameof(queues));
            this._performer = performer ?? throw new ArgumentNullException(nameof(performer));
            this._storage = storage ?? throw new ArgumentNullException(nameof(storage));
            RegisterWorker();
        }

        public void Execute(BackgroundProcessContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var queueJob = _storage.FetchNextJob(this._queues, this._workerId);
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
                    //schedule new run
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
                    //final failed state
                    _storage.InsertAndSetJobState(queueJob.JobId, resultState);
                    if(queueJob.WorkflowId.HasValue)
                    {
                        //mark dependent jobs consequently failed
                        _storage.MarkConsequentlyFailedJobs(queueJob.JobId);
                    }
                }
            }
            else
            {
                //Succeeded
                _storage.InsertAndSetJobState(queueJob.JobId, resultState);
                _storage.TriggerNextJobs(queueJob.JobId);
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
                    _storage.Requeue(performContext.QueueJob.QueueJobId);
                    throw;
                }

                return new FailedState(ex)
                {
                    Reason = "An exception occurred during processing of a background job."
                };
            }        
        }

        private void RegisterWorker()
        {
            _storage.RegisterWorker(_workerId, _serverId);
        }
        
        public override string ToString()
        {
            return $"{GetType().Name} #{_workerId.Substring(0, 8)}";
        }
    }
}
