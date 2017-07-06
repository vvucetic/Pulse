using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pulse.SqlStorage;
using Pulse.Core.Common;
using System.Diagnostics;
using Pulse.Core.Server;
using System.Threading;
using System.Linq;

namespace Pulse.Core.Tests
{
    [TestClass]
    public class CoreTests
    {
        [TestMethod]
        public void JobFromExpression()
        {
            var job = Job.FromExpression(() => Method(4, DateTime.UtcNow));
        }

        [TestMethod]
        public void EnqueueTest()
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage("db");
            var client = new BackgroundJobClient();
            for (int i = 0; i < 100; i++)
            {
                var id = client.Enqueue(() => Method(1, DateTime.UtcNow));

            }
        }

        [TestMethod]
        public void TestServer()
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage("db");
            var server = new BackgroundJobServer();
            Thread.Sleep(TimeSpan.FromDays(1));
        }

        public void Method(int i, DateTime date)
        {
            Thread.Sleep(5000);
            Debug.WriteLine("Wooorks!");
            Debug.WriteLine("Nooo!");
            throw new Exception("Custom exception");
        }

        [TestMethod]
        public void CreateRecurringJob()
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage("db");

            RecurringJob.AddOrUpdate(() => RecurringMethod("Recurring task", 1), Cron.MinuteInterval(2));
        }

        public void RecurringMethod(string message, int id)
        {
            Debug.WriteLine($"Recurring task - {message}!");
        }

        [TestMethod]
        public void Workflow()
        {
            var rootJob = WorkflowJob.MakeJob(() => RecurringMethod("1 task", 1));

            rootJob.ContinueWith(WorkflowJob.MakeJob(() => RecurringMethod("2 task", 2)))
                .ContinueWithGroup(
                    WorkflowJobGroup.RunInParallel(
                            WorkflowJob.MakeJob(() => RecurringMethod("3 task", 3)),
                            WorkflowJob.MakeJob(() => RecurringMethod("4 task", 4))
                        )).ContinueWith(
                WorkflowJob.MakeJob(() => RecurringMethod("5 task", 5)
                ));

            var wf = new Workflow(rootJob);
            wf.SaveWorkflow((t) => { t.QueueJob.JobId = (int)t.QueueJob.Job.Arguments[1]; Debug.WriteLine($"SAved: {t.QueueJob.JobId} - {t.QueueJob.Job.Arguments[0]}"); return (int)t.QueueJob.Job.Arguments[1]; });

            var jobs = wf.GetAllJobs().ToList();
        }

        [TestMethod]
        public void CreateWorkflow()
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage("db");
            var rootJob = WorkflowJob.MakeJob(() => RecurringMethod("1 task", 1));

            //rootJob.ContinueWith(WorkflowJob.MakeJob(() => RecurringMethod("2 task", 2))
            //    .ContinueWithGroup(
            //        WorkflowJob.MakeJob(() => RecurringMethod("3 task", 3)),
            //        WorkflowJob.MakeJob(() => RecurringMethod("4 task", 4))
            //    ).ContinueWith(WorkflowJob.MakeJob(() => RecurringMethod("5 task", 5))));

            var wf = new Workflow(rootJob);
            var client = new BackgroundJobClient();
            client.CreateAndEnqueue(wf);
        }

        [TestMethod]
        public void CreateWorkflow2()
        {
            //GlobalConfiguration.Configuration.UseSqlServerStorage("db");
            //var rootJobGroup = WorkflowJobGroup.RunInParallel( 
            //    WorkflowJob.MakeJob(() => WorkflowMethod("1.1 task")),
            //    WorkflowJob.MakeJob(() => WorkflowMethod("1.2 task")),
            //    WorkflowJob.MakeJob(() => WorkflowMethod("1.3 task")),
            //    WorkflowJob.MakeJob(() => WorkflowMethod("1.4 task")),
            //    WorkflowJob.MakeJob(() => WorkflowMethod("1.5 task"))
            //        //.ContinueWith(WorkflowJob.MakeJob(() => WorkflowMethod("1.5.1 task")))
            //        //    .ContinueWithGroup(
            //        //        WorkflowJobGroup.RunInParallel(
            //        //                WorkflowJob.MakeJob(() => WorkflowMethod("1.5.1.1 task"))
            //        //            )
            //        //    )
            //        //,
            //    WorkflowJob.MakeJob(() => WorkflowMethod("1.6 task")),
            //    WorkflowJob.MakeJob(() => WorkflowMethod("1.7 task"))
            //    );



            //var wf = new Workflow(rootJobGroup);
            //var client = new BackgroundJobClient();
            //client.CreateAndEnqueue(wf);
        }

        private void WorkflowMethod(string message)
        {
            Debug.WriteLine($"Workflow method - {message}!");
        }

        [TestMethod]
        public void TestSerialization()
        {
            var rootJob = WorkflowJob.MakeJob(() => RecurringMethod("1 task", 1));
            var test = JobHelper.ToJson(rootJob.QueueJob);
        }
    }
}
