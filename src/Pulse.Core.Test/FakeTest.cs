using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Threading;
using Pulse.Core.Common;
using Pulse.SqlStorage;
using System.Linq;
using Pulse.Core.Server;

namespace Pulse.Core.Test
{
    [TestClass]
    public class FakeTest
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
            var server = new BackgroundJobServer(new BackgroundJobServerOptions()
            {
                Queues = new[] { "default" }
            });
            System.Threading.Tasks.Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(10000);
                    RecurringJob.Trigger("test workflow");
                }
            });
            Thread.Sleep(TimeSpan.FromDays(1));
        }

        public void Method(int i, DateTime date)
        {
            Thread.Sleep(65000);
        }

        [TestMethod]
        public void CreateRecurringJob()
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage("db");

            RecurringJob.AddOrUpdate(() => RecurringMethod("Recurring task", 1), Cron.MinuteInterval(1), onlyIfLastFinishedOrFailed: true);
        }

        public void RecurringMethod(string message, int id)
        {
            Thread.Sleep(65000);
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

            var job1 = WorkflowJob.MakeJob(() => WorkflowMethod("1 task"));
            var job2 = WorkflowJob.MakeJob(() => WorkflowMethod("2 task"));
            var job31 = WorkflowJob.MakeJob(() => WorkflowMethod("3.1 task"));
            var job32 = WorkflowJob.MakeJob(() => WorkflowMethod("3.2 task"));
            var job321 = WorkflowJob.MakeJob(() => WorkflowMethod("3.2.1 task"));
            var group = WorkflowJobGroup.RunInParallel(
                job31,
                job32
                );

            var job4 = WorkflowJob.MakeJob(() => WorkflowMethod("4 task"));
            var job5 = WorkflowJob.MakeJob(() => WorkflowMethod("5 task"));

            job1.ContinueWith(job2);
            job2.ContinueWithGroup(group);
            group.ContinueWith(job4);
            job4.ContinueWith(job5);
            job32.ContinueWith(job321);

            //rootJob.ContinueWith(WorkflowJob.MakeJob(() => RecurringMethod("2 task", 2))
            //    .ContinueWithGroup(
            //        WorkflowJob.MakeJob(() => RecurringMethod("3 task", 3)),
            //        WorkflowJob.MakeJob(() => RecurringMethod("4 task", 4))
            //    ).ContinueWith(WorkflowJob.MakeJob(() => RecurringMethod("5 task", 5))));

            var wf = new Workflow(job1);
            var client = new BackgroundJobClient();
            client.CreateAndEnqueue(wf);
        }

        [TestMethod]
        public void CreateWorkflow2()
        {
            var dl = WorkflowJob.MakeJob(() => WorkflowMethod("Download"));
            var co1 = WorkflowJob.MakeJob(() => FailingWorkflowMethod("Do something 1"), maxRetries: 1);
            var co2 = WorkflowJob.MakeJob(() => WorkflowMethod("Do something 2"));
            var co3 = WorkflowJob.MakeJob(() => WorkflowMethod("Do something 3"));
            var se1 = WorkflowJob.MakeJob(() => WorkflowMethod("Send email 1"));
            var se3 = WorkflowJob.MakeJob(() => WorkflowMethod("Send email 3"));
            var de = WorkflowJob.MakeJob(() => WorkflowMethod("Delete email"));

            de.WaitFor(se1, co2, se3);
            co1.ContinueWith(se1);
            co3.ContinueWith(se3);
            var group = WorkflowJobGroup.RunInParallel(co1, co2, co3);
            dl.ContinueWithGroup(group);

            var wf = new Workflow(dl);
            GlobalConfiguration.Configuration.UseSqlServerStorage("db");

            var client = new BackgroundJobClient();
            client.CreateAndEnqueue(wf);
        }

        [TestMethod]
        public void CreateRecurringWorkflow()
        {
            var dl = WorkflowJob.MakeJob(() => WorkflowMethod("Download"));
            var co1 = WorkflowJob.MakeJob(() => WorkflowMethod("Do something 1"));
            var co2 = WorkflowJob.MakeJob(() => WorkflowMethod("Do something 2"));
            var co3 = WorkflowJob.MakeJob(() => WorkflowMethod("Do something 3"));
            var se1 = WorkflowJob.MakeJob(() => WorkflowMethod("Send email 1"));
            var se3 = WorkflowJob.MakeJob(() => WorkflowMethod("Send email 3"));
            var de = WorkflowJob.MakeJob(() => WorkflowMethod("Delete email"));

            de.WaitFor(se1, co2, se3);
            co1.ContinueWith(se1);
            co3.ContinueWith(se3);
            var group = WorkflowJobGroup.RunInParallel(co1, co2, co3);
            dl.ContinueWithGroup(group);

            var wf = new Workflow(dl);
            GlobalConfiguration.Configuration.UseSqlServerStorage("db");
            RecurringWorkflow.AddOrUpdate("test workflow", wf, Cron.MinuteInterval(1));
        }

        private void WorkflowMethod(string message)
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            Thread.Sleep(rand.Next(1000, 10000));
            Debug.WriteLine($"Workflow method - {message}!");
        }

        private void FailingWorkflowMethod(string message)
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            Thread.Sleep(rand.Next(1000, 10000));
            throw new Exception("Failed with: " + message);
        }

        [TestMethod]
        public void TestSerialization()
        {
            var rootJob = WorkflowJob.MakeJob(() => RecurringMethod("1 task", 1));
            var test = JobHelper.ToJson(rootJob.QueueJob);
        }
    }
}
