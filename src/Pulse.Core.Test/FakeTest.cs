using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Threading;
using Pulse.Core.Common;
using Pulse.SqlStorage;
using System.Linq;
using Pulse.Core.Server;
using Pulse.Core.Storage;

namespace Pulse.Core.Test
{
    
    //[TestClass]
    public class FakeTest
    {
        [TestMethod]
        [Ignore]
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

            var contextId = Guid.NewGuid();

            var job1 = WorkflowJob.MakeJob(() => WorkflowMethod("1 task"), contextId: contextId);
            var job2 = WorkflowJob.MakeJob(() => WorkflowMethod("2 task"), contextId: contextId);
            var job31 = WorkflowJob.MakeJob(() => WorkflowMethod("3.1 task"), contextId: contextId);
            var job32 = WorkflowJob.MakeJob(() => WorkflowMethod("3.2 task"), contextId: contextId);
            var job321 = WorkflowJob.MakeJob(() => WorkflowMethod("3.2.1 task"), contextId: contextId);
            var group = WorkflowJobGroup.RunInParallel(
                job31,
                job32
                );

            var job4 = WorkflowJob.MakeJob(() => WorkflowMethod("4 task"), contextId: contextId);
            var job5 = WorkflowJob.MakeJob(() => WorkflowMethod("5 task"), contextId: contextId);

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
            //var rootJob = WorkflowJob.MakeJob(() => RecurringMethod("1 task", 1));
            //var test = JobHelper.ToJson(rootJob.QueueJob);

        }

        [TestMethod]
        public void MonitoringApi()
        {
            var sqlStorage = new SqlStorage.SqlStorage("db", new SqlServerStorageOptions { });
            var monitoringApi = sqlStorage.GetMonitoringApi();
            var jobs = monitoringApi.GetContextJobs(new Guid("2AA9E873-B470-45B9-9B49-FDB27030A6E1"), 0, 100);
        }

        [TestMethod]
        public void Deserialize()
        {
            string json = @"
{'Type':'Unitfly.MFiles.EmailProcessor.Service.Common.Jobs.ProcessJobs, Unitfly.MFiles.EmailProcessor.Service.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null','Method':'ExecuteSendEmailAction','ParameterTypes':'[\'Unitfly.MFiles.EmailProcessor.Core.Actions.SendEmailAction, Unitfly.MFiles.EmailProcessor.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\',\'Unitfly.MFiles.EmailProcessor.Core.Actions.DataModel.ActionContext, Unitfly.MFiles.EmailProcessor.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\',\'System.Guid, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\']','Arguments':'[\'{\\\'Id\\\':\\\'c93f93e6-3711-4c5d-9754-8eef9c6578e3\\\',\\\'Type\\\':\\\'SendEmailAction\\\',\\\'NextActions\\\':[],\\\'Parameters\\\':{\\\'MFilesConnectionId\\\':1,\\\'MFilesObjectClassId\\\':80,\\\'OutgoingEmailAccountId\\\':1,\\\'AdditionalRecepients\\\':[],\\\'BodyTemplate\\\':\\\'Prvi email\\\\r\\\\n\\\\r\\\\nLP,\\\\r\\\\n\\\\r\\\\nEmail Processor\\\',\\\'SubjectTemplate\\\':\\\'%original-subject%\\\',\\\'IncludeRecepientsOfOriginalEmail\\\':true,\\\'IncludeSenderOfOriginalEmail\\\':true,\\\'IncludeOriginalMessage\\\':false,\\\'SendAsHtml\\\':true}}\',\'{\\\'Input\\\':null,\\\'ContextEmail\\\':{\\\'UniqueId\\\':{\\\'Id\\\':0,\\\'Validity\\\':0,\\\'IsValid\\\':false},\\\'UidValidity\\\':1,\\\'Email\\\':{\\\'MessageId\\\':\\\'CACyUzr2nez4ZYO+oAHwU=RKp7E8cdUyvqtfM9V8eo11XKJ-mKg@mail.gmail.com\\\',\\\'Bcc\\\':[],\\\'Body\\\':\\\'---------- Forwarded message ----------\\\\r\\\\nFrom: HackerRank Team <hackers@hackerrankmail.com>\\\\r\\\\nDate: 3 August 2017 at 16:51\\\\r\\\\nSubject: Improve your Coding Skills with The Power Sum\\\\r\\\\nTo: vedran.vucetic@gmail.com\\\\r\\\\n\\\\r\\\\n\\\\r\\\\nHi Vedran,\\\\r\\\\n\\\\r\\\\nImprove your skills with this challenge recommended for you:\\\\r\\\\n*The Power Sum*\\\\r\\\\n<http://email.postmaster.hackerrankmail.com/c/eJx1UttuqzAQ_Jrkjch3mwceWqX8BlrsJaBwicA07d-fdUKTnOocCYGZ2VnvjB2K3BgQ-64QjFvmmORKcyYP2ml6s5K_S_vx_vHmSs6V3Cl2mZY4wBJxPrTgzzjPMJ4H6PqDn4Z9WzjfOESvGUdnFKtzzgC5EoE7ZRDkvi_aGC87-bYTJT3X6_WlUWpCIKyxpc8dr2YM3Yw-7mTppzHiF62Oacsq4nDpIeKzRJhN1AUqEtaK3ClCz_hN_wq8sTnTeWNzC0xoB8FKA7XVpuZBuEbTzM6TIHYDLhEGmvTINROUDOkSMRFysyB0MqGTDf1vIwTf5nyOl5BYxZkKk_5hiNa_LHFWPcbXr7ZS6SuTrCWMawXa-Bq5Nk3wSgUHPve15xxBMaa9rK1S-U30915bV86e3LpuYGiUsSKgMmAFc8JIBHA8d4BMmtDcJBRKqk2xLAnYktmy-W86RPgW-h7HEy4bEFvMLtMV52xZ70WyXONQeToL6E7jHTo-dBkFOw0DjgFiN91pYZJioNR_WhwxuXohl2mdPW4k11mA7-xni_1cfGKgQQ-fq8fYebr3p8cdj8Xvg_oDx1gDPg>\\\\r\\\\nAlgorithms\\\\r\\\\n| 5,677 submissions Split up a number in a specified manner.\\\\r\\\\nSolve Challenge\\\\r\\\\n<http://email.postmaster.hackerrankmail.com/c/eJx1UttuqzAQ_Jrkjch3mwceWqX8BlrsJaBwicA07d-fdUKTnOocCYGZ2VnvjB2K3BgQ-64QjFvmmORKcyYP2ml6s5K_S_vx_vHmSs6V3Cl2mZY4wBJxPrTgzzjPMJ4H6PqDn4Z9WzjfOESvGUdnFKtzzgC5EoE7ZRDkvi_aGC87-bYTJT3X6_WlUWpCIKyxpc8dr2YM3Yw-7mTppzHiF62Oacsq4nDpIeKzRJhN1AUqEtaK3ClCz_hN_wq8sTnTeWNzC0xoB8FKA7XVpuZBuEbTzM6TIHYDLhEGmvTINROUDOkSMRFysyB0MqGTDf1vIwTf5nyOl5BYxZkKk_5hiNa_LHFWPcbXr7ZS6SuTrCWMawXa-Bq5Nk3wSgUHPve15xxBMaa9rK1S-U30915bV86e3LpuYGiUsSKgMmAFc8JIBHA8d4BMmtDcJBRKqk2xLAnYktmy-W86RPgW-h7HEy4bEFvMLtMV52xZ70WyXONQeToL6E7jHTo-dBkFOw0DjgFiN91pYZJioNR_WhwxuXohl2mdPW4k11mA7-xni_1cfGKgQQ-fq8fYebr3p8cdj8Xvg_oDx1gDPg>\\\\r\\\\nHappy coding,\\\\r\\\\nThe HackerRank team\\\\r\\\\n\\\\r\\\\nYou are receiving this email because you have email notifications enabled\\\\r\\\\non HackerRank account. Unsubscribe from this promotion here\\\\r\\\\n<http://email.postmaster.hackerrankmail.com/c/eJx1U9uO4jAM_Rp4RM49feBhRjP8RuUkzlJBAZV0Zvfv1ykFOmhXKiU99nF8jpO0baxFue62EoQDD0poI0BtjDf8hp14V-7z_fPN74TQaqXhcr6WHq-Fhs0e44GGAU-HHrvjJp779X5roo9AUVuH_HOBUiKXk5JZ6yYirY_bfSmXlXpbyR0_39_fi0K1CIM4lj3_3fB2oNQNFMtK7eL5VOg3rz7qlm2h_nLEQs8UaWdSlzhJOicbrxk90B_-1hita8A02TUOQRqPySmLwRkbRJI-G6vBRyaUrqdrwZ47_RAGJDvDvBo4MzJJkKaKMFWG-bcQhqc-n-1VpLRl4MTKfwji9YskAe2jfbOUVVOXkSqtYsJoNDYGEsbmFLVOHmMTQxSCUAOYqILjIUykn3vNVQU8Y-M4gynzHGUibdFJ8NIqQvSi8UigbMoThU2pubMtd2Nma_5rjrlPelq-TnuqszCobjCermNoj93pcCP9sOXFmIc1k4oEIIMn6bI1Dlyqx9GDzjm51AQjhJGNVn4mPqd_oy9PgHkIrpHbN7c_tXaNQxdoxtSkrr03UbODQ2EN8h1T2euQZRQgvU5JqiAgeSEhewK8V7U0K1tuZS8D5XYR8I_IuEBnK9bD9osS2775GiOVLvIl_vW4sGX7eur-AvEHMfc>.\\\\r\\\\nCopyright © 2016 HackerRank (2300 Geng Road, Suite 250 Palo Alto,\\\\r\\\\nCalifornia 94303), All rights reserved.\\\\r\\\\n\\\',\\\'Cc\\\':[],\\\'From\\\':\\\'\\\\\\\'Vedran Vucetic\\\\\\\' <vedran.vucetic@gmail.com>\\\',\\\'Subject\\\':\\\'Fwd: Improve your Coding Skills with The Power Sum\\\',\\\'Date\\\':\\\'2017-08-03T19:21:32+02:00\\\',\\\'To\\\':[\\\'unitfly.test@gmail.com\\\'],\\\'HasAttachment\\\':false},\\\'EmailPath\\\':\\\'C:\\\\\\\\Projects\\\\\\\\Unitfly.MFiles.EmailProcessor\\\\\\\\repo\\\\\\\\src\\\\\\\\Unitfly.MFiles.EmailProcessor.Service\\\\\\\\bin\\\\\\\\Debug\\\\\\\\temp\\\\\\\\1-1-39.dat\\\'},\\\'ProcessName\\\':\\\'Project process\\\',\\\'ProcessId\\\':15,\\\'ProcessingEmailId\\\':\\\'00000000-0000-0000-0000-000000000000\\\',\\\'ObjectCreationResult\\\':{\\\'Class\\\':80,\\\'Deleted\\\':false,\\\'ObjVer\\\':{\\\'id\\\':64,\\\'objType\\\':101,\\\'version\\\':1},\\\'Title\\\':\\\'Fwd: Improve your Coding Skills with The Power Sum\\\'}}\',\'\\\'34a01555-dd67-4350-94c0-fc3b3158ab87\\\'\']'}
";
            var invData = JobHelper.FromJson<InvocationData>(json);
            var job = invData.Deserialize();
        }
    }
}
