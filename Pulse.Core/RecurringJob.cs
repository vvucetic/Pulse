using Pulse.Core.Common;
using Pulse.Core.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public static class RecurringJob
    {
        private static readonly Lazy<RecurringManager> Instance = new Lazy<RecurringManager>(
                    () => new RecurringManager());

        public static void AddOrUpdate(
            Expression<Action> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate<T>(
            Expression<Action<T>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate(
            Expression<Action> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobName(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate<T>(
            Expression<Action<T>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobName(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate(
            string recurringJobName,
            Expression<Action> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            AddOrUpdate(recurringJobName, methodCall, cronExpression(), timeZone, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate<T>(
            string recurringJobName,
            Expression<Action<T>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            AddOrUpdate(recurringJobName, methodCall, cronExpression(), timeZone, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate(
            string recurringJobName,
            Expression<Action> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobName, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate<T>(
            string recurringJobName,
            Expression<Action<T>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue, 
            bool onlyIfLastFinishedOrFailed = false)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobName, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate(
            Expression<Func<Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate<T>(
            Expression<Func<T, Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate(
            Expression<Func<Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobName(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate<T>(
            Expression<Func<T, Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobName(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate(
            string recurringJobName,
            Expression<Func<Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            AddOrUpdate(recurringJobName, methodCall, cronExpression(), timeZone, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate<T>(
            string recurringJobName,
            Expression<Func<T, Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            AddOrUpdate(recurringJobName, methodCall, cronExpression(), timeZone, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate(
            string recurringJobName,
            Expression<Func<Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobName, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate<T>(
            string recurringJobName,
            Expression<Func<T, Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue,
            bool onlyIfLastFinishedOrFailed = false)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobName, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue, onlyIfLastFinishedOrFailed);
        }

        public static void AddOrUpdate<T>(
            string recurringJobName,
            Expression<Func<T, Task>> methodCall,
            string cronExpression,
            RecurringJobOptions options)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobName, job, cronExpression, options);
        }

        public static void RemoveIfExists(string recurringJobName)
        {
            Instance.Value.RemoveIfExists(recurringJobName);
        }

        public static void Trigger(string recurringJobName)
        {
            Instance.Value.Trigger(recurringJobName);
        }

        private static string GetRecurringJobName(Job job)
        {
            return $"{job.Type.ToGenericTypeString()}.{job.Method.Name}";
        }
    }
}
