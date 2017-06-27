using Pulse.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    internal class CoreBackgroundJobPerformer : IBackgroundJobPerformer
    {
        internal static readonly Dictionary<Type, Func<PerformContext, object>> Substitutions
                    = new Dictionary<Type, Func<PerformContext, object>>
                    {
                { typeof (CancellationToken), x => x.CancellationToken },
                { typeof (PerformContext), x => x }
                    };

        private readonly JobActivator _activator;

        public CoreBackgroundJobPerformer(JobActivator activator)
        {
            if (activator == null) throw new ArgumentNullException(nameof(activator));
            _activator = activator;
        }

        public object Perform(PerformContext context)
        {
            using (var scope = _activator.BeginScope())
            {
                object instance = null;

                if (context.QueueJob.Job == null)
                {
                    throw new InvalidOperationException("Can't perform a background job with a null job.");
                }

                if (!context.QueueJob.Job.Method.IsStatic)
                {
                    instance = scope.Resolve(context.QueueJob.Job.Type);

                    if (instance == null)
                    {
                        throw new InvalidOperationException(
                            $"JobActivator returned NULL instance of the '{context.QueueJob.Job.Type}' type.");
                    }
                }

                var arguments = SubstituteArguments(context);
                var result = InvokeMethod(context, instance, arguments);

                return result;
            }
        }

        internal static void HandleJobPerformanceException(Exception exception, CancellationToken shutdownToken)
        {
            //if (exception is JobAbortedException)
            //{
            //    // JobAbortedException exception should be thrown as-is to notify
            //    // a worker that background job was aborted by a state change, and
            //    // should NOT be re-queued.
            //    ExceptionDispatchInfo.Capture(exception).Throw();
            //}

            if (exception is OperationCanceledException && shutdownToken.IsCancellationRequested)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw exception;
            }

            // Other exceptions are wrapped with JobPerformanceException to preserve a
            // shallow stack trace without Hangfire methods.
            throw new JobPerformanceException(
                "An exception occurred during performance of the job.",
                exception);
        }

        private static object InvokeMethod(PerformContext context, object instance, object[] arguments)
        {
            try
            {
                var methodInfo = context.QueueJob.Job.Method;
                var result = methodInfo.Invoke(instance, arguments);

                var task = result as Task;

                if (task != null)
                {
                    task.Wait();

                    if (methodInfo.ReturnType.GetTypeInfo().IsGenericType)
                    {
                        var resultProperty = methodInfo.ReturnType.GetRuntimeProperty("Result");

                        result = resultProperty.GetValue(task);
                    }
                    else
                    {
                        result = null;
                    }
                }

                return result;
            }
            catch (ArgumentException ex)
            {
                HandleJobPerformanceException(ex, context.CancellationToken);
                throw;
            }
            catch (AggregateException ex)
            {
                HandleJobPerformanceException(ex.InnerException, context.CancellationToken);
                throw;
            }
            catch (TargetInvocationException ex)
            {
                HandleJobPerformanceException(ex.InnerException, context.CancellationToken);
                throw;
            }
        }

        private static object[] SubstituteArguments(PerformContext context)
        {
            if (context.QueueJob.Job == null)
            {
                return null;
            }

            var parameters = context.QueueJob.Job.Method.GetParameters();
            var result = new List<object>(context.QueueJob.Job.Arguments.Count);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var argument = context.QueueJob.Job.Arguments[i];

                var value = Substitutions.ContainsKey(parameter.ParameterType)
                    ? Substitutions[parameter.ParameterType](context)
                    : argument;

                result.Add(value);
            }

            return result.ToArray();
        }
    }
}
