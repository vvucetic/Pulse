using Moq;
using NFluent;
using Pulse.Core.Common;
using Pulse.Core.States;
using Pulse.SqlStorage.Entities;
using Pulse.SqlStorage.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Pulse.SqlStorage.Tests
{

    public class QueryServiceFacts
    {
        public QueryServiceFacts()
        {
        }

        #region FetchNextJob

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "TestSchema")]
        [InlineData("priority-1", "Pulse")]
        public void Run_FetchNextJob_GetNonNull_WhenValidJobExists(string queueName, string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            var job = CreateValidJob(queueName, schema);
            InsertJobInQueue(job.Id, queueName);
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var workerId = RegisterServerAndWorker(schema);

                var fJob = queryService.FetchNextJob(new[] { queueName }, workerId, db);
                Assert.NotNull(fJob);
                Assert.Equal(queueName, fJob.Queue);
                Assert.Equal(job.Id, fJob.JobId);
                Assert.Equal(workerId, fJob.WorkerId);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "TestSchema")]
        [InlineData("priority-1", "Pulse")]
        public void Run_FetchNextJob_GetNull_WhenJobTableEmpty(string queueName, string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var workerId = RegisterServerAndWorker(schema);

                var job = queryService.FetchNextJob(new[] { queueName }, workerId, db);
                Assert.Null(job);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "TestSchema")]
        [InlineData("priority-1", "Pulse")]
        public void Run_FetchNextJob_GetNull_WhenJobAlreadyFetched(string queueName, string schema)
        { 
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());            
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var workerId = RegisterServerAndWorker(schema);

                var job = CreateValidJob(queueName, schema);
                InsertFetchedJobInQueue(job.Id, queueName, workerId.ToString());

                var fJob = queryService.FetchNextJob(new[] { queueName }, workerId, db);
                Assert.Null(fJob);
            }
        }


        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "priority-1", "TestSchema")]
        [InlineData("priority-2", "test2", "Pulse")]
        public void Run_FetchNextJob_GetNonNull_WhenValidJobExistsInAnyQueue(string queueName1, string queueName2, string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var workerId = RegisterServerAndWorker(schema);

                var job1 = CreateValidJob(queueName1, schema);
                InsertJobInQueue(job1.Id, queueName1);
                var job2 = CreateValidJob(queueName2, schema);
                InsertFetchedJobInQueue(job2.Id, queueName2, workerId);

                var fJob = queryService.FetchNextJob(new[] { queueName1, queueName2 }, workerId, db);
                Assert.NotNull(fJob);
                Assert.Equal(queueName1, fJob.Queue);
                Assert.Equal(job1.Id, fJob.JobId);       
                Assert.Equal(workerId, fJob.WorkerId);
            }
        }

        #endregion

        #region EnqueueNextDelayedJob

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "Pulse")]
        [InlineData("priority-2", "TestSchema")]
        public void Run_EnqueueNextDelayedJob_GetNonNull_WhenJobIsPastDue(string queueName, string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var job = CreateValidJob(queueName, schema);

                var fJob = queryService.EnqueueNextDelayedJob(db);
                Assert.NotNull(fJob);
                Assert.Equal(queueName, fJob.Queue);
                Assert.Equal(job.Id, fJob.JobId);
                Assert.Null(fJob.WorkerId);                
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "Pulse")]
        [InlineData("priority-2", "TestSchema")]
        public void Run_EnqueueNextDelayedJob_GetNull_WhenJobIsNotPastDue(string queueName, string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var job = CreateValidJob(queueName, schema, nextRetry: DateTime.Today.AddDays(2));

                var fJob = queryService.EnqueueNextDelayedJob(db);
                Assert.Null(fJob);
            }
        }

        #endregion

        #region GetJob

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "Pulse")]
        [InlineData("priority-2", "TestSchema")]
        public void Run_GetJob_ReturnsNonNullEqualJob(string queueName, string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var job = CreateValidJob(queueName, schema);

                var fJob = queryService.GetJob(job.Id, db);
                Assert.NotNull(fJob);
                Check.That(fJob).HasFieldsWithSameValues(job);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "Pulse")]
        [InlineData("priority-2", "TestSchema")]
        public void Run_GetJob_RetunsNull_WhenJobDoesntExist(string queueName, string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var fJob = queryService.GetJob(5, db);
                Assert.Null(fJob);
            }
        }

        #endregion

        #region RemoveTimedOutServers

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_RemoveTimedOutServers_RemovesOneServer_WhenTimeouted(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                RegisterServerAndWorker(schema);
                Thread.Sleep(1000);
                var result = queryService.RemoveTimedOutServers(TimeSpan.FromMilliseconds(500), db);
                Assert.Equal(1,result);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_RemoveTimedOutServers_RemovesNoneServer_WhenNotTimeouted(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                RegisterServerAndWorker(schema);
                var result = queryService.RemoveTimedOutServers(TimeSpan.FromMilliseconds(1500), db);
                Assert.Equal(0, result);
            }
        }

        #endregion

        #region HeartbeatServer

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_HeartbeatServer_InsertsNewServer_WhenServerDoesntExist(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                queryService.HeartbeatServer("server1", "data1", db);
                var server = db.Query<ServerEntity>().FirstOrDefault();
                Assert.NotNull(server);
                Assert.Equal("server1", server.Id);
                Assert.Equal("data1", server.Data);
                Assert.True(server.LastHeartbeat.Subtract(DateTime.UtcNow) < TimeSpan.FromMinutes(1));
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_HeartbeatServer_UpdateServer_WhenServerExists(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                queryService.HeartbeatServer("server1", "data1", db);
                queryService.HeartbeatServer("server1", "data3", db);
                var server = db.Query<ServerEntity>().FirstOrDefault();
                Assert.NotNull(server);
                Assert.Equal("server1", server.Id);
                Assert.Equal("data3", server.Data);
                Assert.True(server.LastHeartbeat.Subtract(DateTime.UtcNow) < TimeSpan.FromMinutes(1));
            }
        }

        #endregion

        #region RemoveServer

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_RemoveServer_RemovesServer_WhenServerExists(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                queryService.HeartbeatServer("server1", "data1", db);
                var result = queryService.RemoveServer("server1", db);
                Assert.Equal(1, result);
                var server = db.Query<ServerEntity>().FirstOrDefault();
                Assert.Null(server);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_RemoveServer_RemovesServer_WhenServerDoesntExist(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var result = queryService.RemoveServer("server1", db);
                Assert.Equal(0, result);
            }
        }

        #endregion

        #region LockFirstScheduledItem

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_LockFirstScheduledItem_ReturnsNotNull_WhenPassedDueDateAndParallelRunningAllowed(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            CreateScheduledEntity(false, DateTime.Today.AddDays(-1));
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var returnedScheduledItem = queryService.LockFirstScheduledItem(db);
                Assert.NotNull(returnedScheduledItem);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_LockFirstScheduledItem_ReturnsNull_WhenPassedNotDueDateAndParallelRunningAllowed(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            CreateScheduledEntity(false, DateTime.Today.AddDays(1));
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var returnedScheduledItem = queryService.LockFirstScheduledItem(db);
                Assert.Null(returnedScheduledItem);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse", "test schedule", ProcessingState.DefaultName)]
        [InlineData("TestSchema", "testschedule", ProcessingState.DefaultName)]
        [InlineData("TestSchema", "testschedule1", ScheduledState.DefaultName)]
        [InlineData("Pulse", "testschedule", AwaitingState.DefaultName)]
        [InlineData("TestSchema", "tests-chedule", EnqueuedState.DefaultName)]
        public void Run_LockFirstScheduledItem_ReturnsNotNull_WhenPassedDueDateAndParallelRunningAllowedAndForbiddenStates(string schema, string scheduleName, string state)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            var se = CreateScheduledEntity(false, DateTime.Today.AddDays(-1), scheduleName);
            CreateValidJob("default", schema, scheduleName: scheduleName, state: state);
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var returnedScheduledItem = queryService.LockFirstScheduledItem(db);
                Assert.NotNull(returnedScheduledItem);
                Assert.NotEqual(returnedScheduledItem.LastInvocation, se.LastInvocation);
                //make this fields same in order to next check to pass. Last invocation is always changed in LockFirstScheduledItem
                returnedScheduledItem.LastInvocation = se.LastInvocation;
                Check.That(returnedScheduledItem).HasFieldsWithSameValues(se);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse", "test schedule", ProcessingState.DefaultName)]
        [InlineData("TestSchema", "testschedule", ProcessingState.DefaultName)]
        [InlineData("TestSchema", "testschedule1", ScheduledState.DefaultName)]
        [InlineData("Pulse", "testschedule", AwaitingState.DefaultName)]
        [InlineData("TestSchema", "tests-chedule", EnqueuedState.DefaultName)]
        public void Run_LockFirstScheduledItem_ReturnsNull_WhenPassedDueDateAndParallelRunningNotAllowedAndForbiddenStates(string schema, string scheduleName, string state)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            var se = CreateScheduledEntity(true, DateTime.Today.AddDays(-1), scheduleName);
            CreateValidJob("default", schema, scheduleName: scheduleName, state: state);
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var returnedScheduledItem = queryService.LockFirstScheduledItem(db);
                Assert.Null(returnedScheduledItem);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse", "test schedule", FailedState.DefaultName)]
        [InlineData("TestSchema", "testschedule", SucceededState.DefaultName)]
        [InlineData("TestSchema", "testschedule1", ConsequentlyFailed.DefaultName)]
        public void Run_LockFirstScheduledItem_ReturnsNotNull_WhenPassedDueDateAndParallelRunningNotAllowedAndAllowedStates(string schema, string scheduleName, string state)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            var se = CreateScheduledEntity(true, DateTime.Today.AddDays(-1), scheduleName);
            CreateValidJob("default", schema, scheduleName: scheduleName, state: state);
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var returnedScheduledItem = queryService.LockFirstScheduledItem(db);
                Assert.NotNull(returnedScheduledItem);
                Assert.NotEqual(returnedScheduledItem.LastInvocation, se.LastInvocation);
                //make this fields same in order to next check to pass. Last invocation is always changed in LockFirstScheduledItem
                returnedScheduledItem.LastInvocation = se.LastInvocation;
                Check.That(returnedScheduledItem).HasFieldsWithSameValues(se);
            }
        }

        #endregion

        #region LockScheduledItem

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_LockScheduledItem_ReturnsNull_WhenDoesntExist(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            //CreateScheduledEntity(false, DateTime.Today.AddDays(1));
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var returnedScheduledItem = queryService.LockScheduledItem("item1", db);
                Assert.Null(returnedScheduledItem);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_LockScheduledItem_ReturnsNotNull_WhenExists(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            var sc1 = CreateScheduledEntity(false, DateTime.Today.AddDays(1), scheduleName: "sch1");
            var sc2 = CreateScheduledEntity(false, DateTime.Today.AddDays(1), scheduleName: "sch2");
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var returnedScheduledItem = queryService.LockScheduledItem(sc1.Name, db);
                Assert.NotNull(returnedScheduledItem);
                Assert.NotEqual(returnedScheduledItem.LastInvocation, sc1.LastInvocation);
                //make this fields same in order to next check to pass. Last invocation is always changed in LockFirstScheduledItem
                returnedScheduledItem.LastInvocation = sc1.LastInvocation;
                Check.That(returnedScheduledItem).HasFieldsWithSameValues(sc1);
            }
        }

        #endregion

        #region CreateOrUpdateRecurringJob

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithJob_WhenExists(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var sc = new Core.Common.ScheduledTask()
                {
                    Cron = "0 0 0 0",
                    Job = new Core.Common.QueueJob()
                    {
                        Description = "test",
                        Job = Job.FromExpression(()=> Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithJob_WhenExists("")),
                        ContextId = Guid.NewGuid(),
                        MaxRetries = 10,
                        QueueName = "dflt"
                    },
                    Name = "recurring_name",
                    OnlyIfLastFinishedOrFailed = true,
                    LastInvocation = DateTime.Today,
                    NextInvocation = DateTime.Today
                };
                var result = queryService.CreateOrUpdateRecurringJob(sc, db);
                Assert.Equal(1, result);
                var recurring = db.Query<ScheduleEntity>().FirstOrDefault();
                Assert.Equal(sc.Cron, recurring.Cron);
                Assert.Equal(sc.OnlyIfLastFinishedOrFailed, recurring.OnlyIfLastFinishedOrFailed);
                Assert.Equal(sc.LastInvocation, recurring.LastInvocation);
                Assert.Equal(sc.Name, recurring.Name);
                Assert.Equal(sc.NextInvocation, recurring.NextInvocation);
                Assert.Equal(JobHelper.ToJson(sc.Workflow), recurring.WorkflowInvocationData);
                Assert.Equal(JobHelper.ToJson(ScheduledJobInvocationData.FromScheduledJob(sc)), recurring.JobInvocationData);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithWorkflow_WhenExists(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var job1 = WorkflowJob.MakeJob(() => Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithWorkflow_WhenExists(""), "dflt2", Guid.NewGuid(), 4, "new");
                var job2 = WorkflowJob.MakeJob(() => Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithWorkflow_WhenExists("asd"), "dfldasdt2", Guid.NewGuid(), 2, "new2");
                job1.ContinueWith(job2);
                var sc = new Core.Common.ScheduledTask()
                {
                    Cron = "0 0 0 0",
                    Workflow = new Workflow(job1),
                    Name = "recurring_name",
                    OnlyIfLastFinishedOrFailed = true,
                    LastInvocation = DateTime.Today,
                    NextInvocation = DateTime.Today
                };
                var result = queryService.CreateOrUpdateRecurringJob(sc, db);
                Assert.Equal(1, result);
                var recurring = db.Query<ScheduleEntity>().FirstOrDefault();
                Assert.Equal(sc.Cron, recurring.Cron);
                Assert.Equal(sc.OnlyIfLastFinishedOrFailed, recurring.OnlyIfLastFinishedOrFailed);
                Assert.Equal(sc.LastInvocation, recurring.LastInvocation);
                Assert.Equal(sc.Name, recurring.Name);
                Assert.Equal(sc.NextInvocation, recurring.NextInvocation);
                Assert.Equal(JobHelper.ToJson(sc.Workflow), recurring.WorkflowInvocationData);
                Assert.Equal(JobHelper.ToJson(ScheduledJobInvocationData.FromScheduledJob(sc)), recurring.JobInvocationData);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_CreateOrUpdateRecurringJob_UpdatesExistingRecurringJob(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var sc = new Core.Common.ScheduledTask()
                {
                    Cron = "0 0 0 0",
                    Job = new Core.Common.QueueJob()
                    {
                        Description = "test",
                        Job = Job.FromExpression(() => Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithJob_WhenExists("")),
                        ContextId = Guid.NewGuid(),
                        MaxRetries = 10,
                        QueueName = "dflt"
                    },
                    Name = "recurring_name",
                    OnlyIfLastFinishedOrFailed = true,
                    LastInvocation = DateTime.Today,
                    NextInvocation = DateTime.Today
                };
                var result = queryService.CreateOrUpdateRecurringJob(sc, db);
                var sc2 = new Core.Common.ScheduledTask()
                {
                    Cron = "0 0 0 0 0",
                    Job = new Core.Common.QueueJob()
                    {
                        Description = "test 2",
                        Job = Job.FromExpression(() => Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithJob_WhenExists("")),
                        ContextId = Guid.NewGuid(),
                        MaxRetries = 2,
                        QueueName = "dfdsasdlt"
                    },
                    Name = "recurring_name",
                    OnlyIfLastFinishedOrFailed = false,
                    LastInvocation = DateTime.Today,
                    NextInvocation = DateTime.Today
                };
                var result2 = queryService.CreateOrUpdateRecurringJob(sc2, db);

                Assert.Equal(1, result2);
                var recurring = db.Query<ScheduleEntity>().FirstOrDefault();
                Assert.Equal(sc2.Cron, recurring.Cron);
                Assert.Equal(sc2.OnlyIfLastFinishedOrFailed, recurring.OnlyIfLastFinishedOrFailed);
                Assert.Equal(sc2.LastInvocation, recurring.LastInvocation);
                Assert.Equal(sc2.Name, recurring.Name);
                Assert.Equal(sc2.NextInvocation, recurring.NextInvocation);
                Assert.Equal(JobHelper.ToJson(sc2.Workflow), recurring.WorkflowInvocationData);
                Assert.Equal(JobHelper.ToJson(ScheduledJobInvocationData.FromScheduledJob(sc2)), recurring.JobInvocationData);
            }
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_CreateOrUpdateRecurringJob_UpdatesExistingRecurringWorkflow(string schema)
        {
            CustomDatabaseFactory.Setup(schema, ConnectionUtils.GetConnectionString());
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var job1 = WorkflowJob.MakeJob(() => Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithWorkflow_WhenExists(""), "dflt2", Guid.NewGuid(), 4, "new");
                var job2 = WorkflowJob.MakeJob(() => Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithWorkflow_WhenExists("asd"), "dfldasdt2", Guid.NewGuid(), 2, "new2");
                job1.ContinueWith(job2);
                var sc = new Core.Common.ScheduledTask()
                {
                    Cron = "0 0 0 0",
                    Workflow = new Workflow(job1),
                    Name = "recurring_name",
                    OnlyIfLastFinishedOrFailed = true,
                    LastInvocation = DateTime.Today,
                    NextInvocation = DateTime.Today
                };
                var result = queryService.CreateOrUpdateRecurringJob(sc, db);

                job1 = WorkflowJob.MakeJob(() => Run_CreateOrUpdateRecurringJob_UpdatesExistingRecurringWorkflow(""), "dflt2", Guid.NewGuid(), 4, "new");
                job2 = WorkflowJob.MakeJob(() => Run_CreateOrUpdateRecurringJob_UpdatesExistingRecurringWorkflow("asd"), "dfldasdt2", Guid.NewGuid(), 2, "new2");
                job1.ContinueWith(job2);
                var sc2 = new Core.Common.ScheduledTask()
                {
                    Cron = "0 0 0 0 12",
                    Workflow = new Workflow(job1),
                    Name = "recurring_name",
                    OnlyIfLastFinishedOrFailed = false,
                    LastInvocation = DateTime.Today,
                    NextInvocation = DateTime.Today
                };
                var result2 = queryService.CreateOrUpdateRecurringJob(sc2, db);
                Assert.Equal(1, result2);
                var recurring = db.Query<ScheduleEntity>().FirstOrDefault();
                Assert.Equal(sc2.Cron, recurring.Cron);
                Assert.Equal(sc2.OnlyIfLastFinishedOrFailed, recurring.OnlyIfLastFinishedOrFailed);
                Assert.Equal(sc2.LastInvocation, recurring.LastInvocation);
                Assert.Equal(sc2.Name, recurring.Name);
                Assert.Equal(sc2.NextInvocation, recurring.NextInvocation);
                Assert.Equal(JobHelper.ToJson(sc2.Workflow), recurring.WorkflowInvocationData);
                Assert.Equal(JobHelper.ToJson(ScheduledJobInvocationData.FromScheduledJob(sc2)), recurring.JobInvocationData);
            }
        }

        #endregion

        #region Helpers

        private JobEntity CreateValidJob(string queueName, string schema, DateTime? nextRetry = null, string state = null, string scheduleName = null)
        {
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
                var job = new JobEntity()
                {
                    InvocationData = "{}",
                    NextJobs = "[]",
                    Description = "Description",
                    ContextId = Guid.NewGuid(),
                    CreatedAt = DateTime.Today,
                    ExpireAt = DateTime.Today,
                    MaxRetries = Guid.NewGuid().GetHashCode(),
                    NextRetry = nextRetry ?? DateTime.Today.AddDays(-2),
                    Queue = queueName, 
                    RetryCount = Guid.NewGuid().GetHashCode(),
                    ScheduleName = scheduleName ?? "schname",
                    State = state??"newState",
                    WorkflowId = Guid.NewGuid()
                };
                db.Insert(job);
                return job;
            }
        }

        private void InsertJobInQueue(int jobId, string queueName)
        {
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var queue = new QueueEntity()
                {
                    JobId = jobId,
                    Queue = queueName,
                };
                db.Insert(queue);
            }
        }

        private void InsertFetchedJobInQueue(int jobId, string queueName, string workerId)
        {
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var queue = new QueueEntity()
                {
                    JobId = jobId,
                    Queue = queueName,
                    WorkerId = workerId
                };
                db.Insert(queue);
            }
        }

        private string RegisterServerAndWorker(string schema)
        {
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
                var serverId = Guid.NewGuid();
                queryService.HeartbeatServer(serverId.ToString(), "", db);
                var workerId = Guid.NewGuid();
                queryService.RegisterWorker(workerId.ToString(), serverId.ToString(), db);
                return workerId.ToString();
            }
        }

        private ScheduleEntity CreateScheduledEntity(bool onlyIfLastFinishedOrFailed = false, DateTime? nextInvocation = null, string scheduleName = null)
        {
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var entity = new ScheduleEntity()
                {
                    Cron = "",
                    LastInvocation = DateTime.Today,
                    Name = scheduleName ?? "dummy",
                    OnlyIfLastFinishedOrFailed = onlyIfLastFinishedOrFailed,
                    NextInvocation = nextInvocation ?? DateTime.Today
                };
                db.Insert<ScheduleEntity>(entity);
                return entity;
            }
        }

#endregion
    }
}
