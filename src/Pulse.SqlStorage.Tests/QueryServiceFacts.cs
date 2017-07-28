using Moq;
using NFluent;
using Pulse.Core.Common;
using Pulse.Core.States;
using Pulse.SqlStorage.Entities;
using Pulse.SqlStorage.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Dapper.Contrib.Extensions;
using Dapper;
using System.Data.Common;
using System.Transactions;

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
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var job = CreateValidJob(conn, queueName, schema);
                InsertJobInQueue(conn, job.Id, queueName);
                var workerId = RegisterServerAndWorker(conn, schema);

                var fJob = queryService.FetchNextJob(new[] { queueName }, workerId, conn);
                Assert.NotNull(fJob);
                Assert.Equal(queueName, fJob.Queue);
                Assert.Equal(job.Id, fJob.JobId);
                Assert.Equal(workerId, fJob.WorkerId);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "TestSchema")]
        [InlineData("priority-1", "Pulse")]
        public void Run_FetchNextJob_GetNull_WhenJobTableEmpty(string queueName, string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                var workerId = RegisterServerAndWorker(conn, schema);

                var job = queryService.FetchNextJob(new[] { queueName }, workerId, conn);
                Assert.Null(job);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "TestSchema")]
        [InlineData("priority-1", "Pulse")]
        public void Run_FetchNextJob_GetNull_WhenJobAlreadyFetched(string queueName, string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                var workerId = RegisterServerAndWorker(conn, schema);

                var job = CreateValidJob(conn, queueName, schema);
                InsertFetchedJobInQueue(conn, job.Id, queueName, workerId.ToString());

                var fJob = queryService.FetchNextJob(new[] { queueName }, workerId, conn);
                Assert.Null(fJob);
            });
        }


        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "priority-1", "TestSchema")]
        [InlineData("priority-2", "test2", "Pulse")]
        public void Run_FetchNextJob_GetNonNull_WhenValidJobExistsInAnyQueue(string queueName1, string queueName2, string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                var workerId = RegisterServerAndWorker(conn, schema);

                var job1 = CreateValidJob(conn, queueName1, schema);
                InsertJobInQueue(conn, job1.Id, queueName1);
                var job2 = CreateValidJob(conn, queueName2, schema);
                InsertFetchedJobInQueue(conn, job2.Id, queueName2, workerId);

                var fJob = queryService.FetchNextJob(new[] { queueName1, queueName2 }, workerId, conn);
                Assert.NotNull(fJob);
                Assert.Equal(queueName1, fJob.Queue);
                Assert.Equal(job1.Id, fJob.JobId);
                Assert.Equal(workerId, fJob.WorkerId);
            });
        }

        #endregion

        #region EnqueueNextDelayedJob

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "Pulse")]
        [InlineData("priority-2", "TestSchema")]
        public void Run_EnqueueNextDelayedJob_GetNonNull_WhenJobIsPastDue(string queueName, string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                var job = CreateValidJob(conn, queueName, schema);

                var fJob = queryService.EnqueueNextDelayedJob(conn);
                Assert.NotNull(fJob);
                Assert.Equal(queueName, fJob.Queue);
                Assert.Equal(job.Id, fJob.JobId);
                Assert.Null(fJob.WorkerId);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "Pulse")]
        [InlineData("priority-2", "TestSchema")]
        public void Run_EnqueueNextDelayedJob_GetNull_WhenJobIsNotPastDue(string queueName, string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                var job = CreateValidJob(conn, queueName, schema, nextRetry: DateTime.Today.AddDays(2));

                var fJob = queryService.EnqueueNextDelayedJob(conn);
                Assert.Null(fJob);
            });
        }

        #endregion

        #region GetJob

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "Pulse")]
        [InlineData("priority-2", "TestSchema")]
        public void Run_GetJob_ReturnsNonNullEqualJob(string queueName, string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                var job = CreateValidJob(conn, queueName, schema);

                var fJob = queryService.GetJob(job.Id, conn);
                Assert.NotNull(fJob);
                Check.That(fJob).HasFieldsWithSameValues(job);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("default", "Pulse")]
        [InlineData("priority-2", "TestSchema")]
        public void Run_GetJob_RetunsNull_WhenJobDoesntExist(string queueName, string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                var fJob = queryService.GetJob(5, conn);
                Assert.Null(fJob);
            });
        }

        #endregion

        #region RemoveTimedOutServers

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_RemoveTimedOutServers_RemovesOneServer_WhenTimeouted(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                RegisterServerAndWorker(conn, schema);
                Thread.Sleep(1000);
                var result = queryService.RemoveTimedOutServers(TimeSpan.FromMilliseconds(500), conn);
                Assert.Equal(1, result);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_RemoveTimedOutServers_RemovesNoneServer_WhenNotTimeouted(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                RegisterServerAndWorker(conn, schema);
                var result = queryService.RemoveTimedOutServers(TimeSpan.FromMilliseconds(1500), conn);
                Assert.Equal(0, result);
            });
        }

        #endregion

        #region HeartbeatServer

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_HeartbeatServer_InsertsNewServer_WhenServerDoesntExist(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                queryService.HeartbeatServer("server1", "data1", conn);
                var server = conn.Query<ServerEntity>($"SELECT * FROM [{schema}].Server").FirstOrDefault();
                Assert.NotNull(server);
                Assert.Equal("server1", server.Id);
                Assert.Equal("data1", server.Data);
                Assert.True(server.LastHeartbeat.Subtract(DateTime.UtcNow) < TimeSpan.FromMinutes(1));
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_HeartbeatServer_UpdateServer_WhenServerExists(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                queryService.HeartbeatServer("server1", "data1", conn);
                queryService.HeartbeatServer("server1", "data3", conn);
                var server = conn.Query<ServerEntity>($"SELECT * FROM [{schema}].Server").FirstOrDefault();
                Assert.NotNull(server);
                Assert.Equal("server1", server.Id);
                Assert.Equal("data3", server.Data);
                Assert.True(server.LastHeartbeat.Subtract(DateTime.UtcNow) < TimeSpan.FromMinutes(1));
            });
        }

        #endregion

        #region RemoveServer

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_RemoveServer_RemovesServer_WhenServerExists(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                queryService.HeartbeatServer("server1", "data1", conn);
                var result = queryService.RemoveServer("server1", conn);
                Assert.True(result);
                var server = conn.Query<ServerEntity>($"SELECT * FROM [{schema}].Server").FirstOrDefault();
                Assert.Null(server);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_RemoveServer_RemovesServer_WhenServerDoesntExist(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });

            UseConnection((conn) =>
            {
                var result = queryService.RemoveServer("server1", conn);
                Assert.False(result);
            });
        }

        #endregion

        #region LockFirstScheduledItem

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_LockFirstScheduledItem_ReturnsNotNull_WhenPassedDueDateAndParallelRunningAllowed(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                CreateScheduledEntity(conn, false, DateTime.Today.AddDays(-1));
                var returnedScheduledItem = queryService.LockFirstScheduledItem(conn);
                Assert.NotNull(returnedScheduledItem);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_LockFirstScheduledItem_ReturnsNull_WhenPassedNotDueDateAndParallelRunningAllowed(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                CreateScheduledEntity(conn, false, DateTime.Today.AddDays(1));
                var returnedScheduledItem = queryService.LockFirstScheduledItem(conn);
                Assert.Null(returnedScheduledItem);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse", "test schedule", ProcessingState.DefaultName)]
        [InlineData("TestSchema", "testschedule", ProcessingState.DefaultName)]
        [InlineData("TestSchema", "testschedule1", ScheduledState.DefaultName)]
        [InlineData("Pulse", "testschedule", AwaitingState.DefaultName)]
        [InlineData("TestSchema", "tests-chedule", EnqueuedState.DefaultName)]
        public void Run_LockFirstScheduledItem_ReturnsNotNull_WhenPassedDueDateAndParallelRunningAllowedAndForbiddenStates(string schema, string scheduleName, string state)
        {

            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var se = CreateScheduledEntity(conn, false, DateTime.Today.AddDays(-1), scheduleName);
                CreateValidJob(conn, "default", schema, scheduleName: scheduleName, state: state);
                var returnedScheduledItem = queryService.LockFirstScheduledItem(conn);
                Assert.NotNull(returnedScheduledItem);
                Assert.NotEqual(returnedScheduledItem.LastInvocation, se.LastInvocation);
                //make this fields same in order to next check to pass. Last invocation is always changed in LockFirstScheduledItem
                returnedScheduledItem.LastInvocation = se.LastInvocation;
                Check.That(returnedScheduledItem).HasFieldsWithSameValues(se);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse", "test schedule", ProcessingState.DefaultName)]
        [InlineData("TestSchema", "testschedule", ProcessingState.DefaultName)]
        [InlineData("TestSchema", "testschedule1", ScheduledState.DefaultName)]
        [InlineData("Pulse", "testschedule", AwaitingState.DefaultName)]
        [InlineData("TestSchema", "tests-chedule", EnqueuedState.DefaultName)]
        public void Run_LockFirstScheduledItem_ReturnsNull_WhenPassedDueDateAndParallelRunningNotAllowedAndForbiddenStates(string schema, string scheduleName, string state)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var se = CreateScheduledEntity(conn, true, DateTime.Today.AddDays(-1), scheduleName);
                CreateValidJob(conn, "default", schema, scheduleName: scheduleName, state: state);
                var returnedScheduledItem = queryService.LockFirstScheduledItem(conn);
                Assert.Null(returnedScheduledItem);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse", "test schedule", FailedState.DefaultName)]
        [InlineData("TestSchema", "testschedule", SucceededState.DefaultName)]
        [InlineData("TestSchema", "testschedule1", ConsequentlyFailed.DefaultName)]
        public void Run_LockFirstScheduledItem_ReturnsNotNull_WhenPassedDueDateAndParallelRunningNotAllowedAndAllowedStates(string schema, string scheduleName, string state)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var se = CreateScheduledEntity(conn, true, DateTime.Today.AddDays(-1), scheduleName);
                CreateValidJob(conn, "default", schema, scheduleName: scheduleName, state: state);
                var returnedScheduledItem = queryService.LockFirstScheduledItem(conn);
                Assert.NotNull(returnedScheduledItem);
                Assert.NotEqual(returnedScheduledItem.LastInvocation, se.LastInvocation);
                //make this fields same in order to next check to pass. Last invocation is always changed in LockFirstScheduledItem
                returnedScheduledItem.LastInvocation = se.LastInvocation;
                Check.That(returnedScheduledItem).HasFieldsWithSameValues(se);
            });
        }

        #endregion

        #region LockScheduledItem

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_LockScheduledItem_ReturnsNull_WhenDoesntExist(string schema)
        {

            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            //CreateScheduledEntity(false, DateTime.Today.AddDays(1));
            UseConnection((conn) =>
            {
                var returnedScheduledItem = queryService.LockScheduledItem("item1", conn);
                Assert.Null(returnedScheduledItem);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_LockScheduledItem_ReturnsNotNull_WhenExists(string schema)
        {

            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var sc1 = CreateScheduledEntity(conn, false, DateTime.Today.AddDays(1), scheduleName: "sch1");
                var sc2 = CreateScheduledEntity(conn, false, DateTime.Today.AddDays(1), scheduleName: "sch2");
                var returnedScheduledItem = queryService.LockScheduledItem(sc1.Name, conn);
                Assert.NotNull(returnedScheduledItem);
                Assert.NotEqual(returnedScheduledItem.LastInvocation, sc1.LastInvocation);
                //make this fields same in order to next check to pass. Last invocation is always changed in LockFirstScheduledItem
                returnedScheduledItem.LastInvocation = sc1.LastInvocation;
                Check.That(returnedScheduledItem).HasFieldsWithSameValues(sc1);
            });
        }

        #endregion

        #region RemoveScheduledItem

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_RemoveScheduledItem_ReturnsOne_WhenDeleted(string schema)
        {

            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                CreateScheduledEntity(conn, false, DateTime.Today.AddDays(1), scheduleName: "schitem");
                var result = queryService.RemoveScheduledItem("schitem", conn);
                Assert.True(result);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_RemoveScheduledItem_ReturnsZero_WhenNoDeleted(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var result = queryService.RemoveScheduledItem("schitem", conn);
                Assert.False(result);
            });
        }

        #endregion

        #region CreateOrUpdateRecurringJob

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithJob_WhenExists(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
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
                var result = queryService.CreateOrUpdateRecurringJob(sc, conn);
                Assert.Equal(1, result);
                var recurring = conn.Query<ScheduleEntity>($"SELECT * FROM [{schema}].Schedule").FirstOrDefault();
                Assert.Equal(sc.Cron, recurring.Cron);
                Assert.Equal(sc.OnlyIfLastFinishedOrFailed, recurring.OnlyIfLastFinishedOrFailed);
                Assert.Equal(sc.LastInvocation, recurring.LastInvocation);
                Assert.Equal(sc.Name, recurring.Name);
                Assert.Equal(sc.NextInvocation, recurring.NextInvocation);
                Assert.Equal(JobHelper.ToJson(sc.Workflow), recurring.WorkflowInvocationData);
                Assert.Equal(JobHelper.ToJson(ScheduledJobInvocationData.FromScheduledJob(sc)), recurring.JobInvocationData);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_CreateOrUpdateRecurringJob_ReturnsNotNullWithWorkflow_WhenExists(string schema)
        {

            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
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
                var result = queryService.CreateOrUpdateRecurringJob(sc, conn);
                Assert.Equal(1, result);
                var recurring = conn.Query<ScheduleEntity>($"SELECT * FROM [{schema}].Schedule").FirstOrDefault();
                Assert.Equal(sc.Cron, recurring.Cron);
                Assert.Equal(sc.OnlyIfLastFinishedOrFailed, recurring.OnlyIfLastFinishedOrFailed);
                Assert.Equal(sc.LastInvocation, recurring.LastInvocation);
                Assert.Equal(sc.Name, recurring.Name);
                Assert.Equal(sc.NextInvocation, recurring.NextInvocation);
                Assert.Equal(JobHelper.ToJson(sc.Workflow), recurring.WorkflowInvocationData);
                Assert.Equal(JobHelper.ToJson(ScheduledJobInvocationData.FromScheduledJob(sc)), recurring.JobInvocationData);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_CreateOrUpdateRecurringJob_UpdatesExistingRecurringJob(string schema)
        {

            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
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
                var result = queryService.CreateOrUpdateRecurringJob(sc, conn);
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
                var result2 = queryService.CreateOrUpdateRecurringJob(sc2, conn);

                Assert.Equal(1, result2);
                var recurring = conn.Query<ScheduleEntity>($"SELECT * FROM [{schema}].Schedule").FirstOrDefault();
                Assert.Equal(sc2.Cron, recurring.Cron);
                Assert.Equal(sc2.OnlyIfLastFinishedOrFailed, recurring.OnlyIfLastFinishedOrFailed);
                Assert.Equal(sc2.LastInvocation, recurring.LastInvocation);
                Assert.Equal(sc2.Name, recurring.Name);
                Assert.Equal(sc2.NextInvocation, recurring.NextInvocation);
                Assert.Equal(JobHelper.ToJson(sc2.Workflow), recurring.WorkflowInvocationData);
                Assert.Equal(JobHelper.ToJson(ScheduledJobInvocationData.FromScheduledJob(sc2)), recurring.JobInvocationData);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_CreateOrUpdateRecurringJob_UpdatesExistingRecurringWorkflow(string schema)
        {

            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
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
                var result = queryService.CreateOrUpdateRecurringJob(sc, conn);

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
                var result2 = queryService.CreateOrUpdateRecurringJob(sc2, conn);
                Assert.Equal(1, result2);
                var recurring = conn.Query<ScheduleEntity>($"SELECT * FROM [{schema}].Schedule").FirstOrDefault();
                Assert.Equal(sc2.Cron, recurring.Cron);
                Assert.Equal(sc2.OnlyIfLastFinishedOrFailed, recurring.OnlyIfLastFinishedOrFailed);
                Assert.Equal(sc2.LastInvocation, recurring.LastInvocation);
                Assert.Equal(sc2.Name, recurring.Name);
                Assert.Equal(sc2.NextInvocation, recurring.NextInvocation);
                Assert.Equal(JobHelper.ToJson(sc2.Workflow), recurring.WorkflowInvocationData);
                Assert.Equal(JobHelper.ToJson(ScheduledJobInvocationData.FromScheduledJob(sc2)), recurring.JobInvocationData);
            });
        }

        #endregion

        #region MarkAsFinishedAndGetNextJobs

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_MarkAsFinishedAndGetNextJobs_ReturnsNextJob(string schema)
        {

            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var job1 = CreateValidJob(conn, "default1", schema, state: EnqueuedState.DefaultName);
                var job2 = CreateValidJob(conn, "default2", schema, state: AwaitingState.DefaultName);
                var job3 = CreateValidJob(conn, "default3", schema, state: AwaitingState.DefaultName);
                var job4 = CreateValidJob(conn, "default4", schema, state: AwaitingState.DefaultName);
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job2.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job3.Id,
                    ParentJobId = job2.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job4.Id,
                    ParentJobId = job3.Id
                });
                var result = queryService.MarkAsFinishedAndGetNextJobs(job1.Id, conn);
                Check.That(result).HasOneElementOnly().Which.HasFieldsWithSameValues(job2);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_MarkAsFinishedAndGetNextJobs_ReturnsNextJobs(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var job1 = CreateValidJob(conn, "default1", schema, state: EnqueuedState.DefaultName);
                var job2 = CreateValidJob(conn, "default2", schema, state: AwaitingState.DefaultName);
                var job3 = CreateValidJob(conn, "default3", schema, state: AwaitingState.DefaultName);
                var job4 = CreateValidJob(conn, "default4", schema, state: AwaitingState.DefaultName);
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job2.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job3.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job4.Id,
                    ParentJobId = job1.Id
                });
                var result = queryService.MarkAsFinishedAndGetNextJobs(job1.Id, conn);
                Check.That(result).HasElementThatMatches(t => t.Id == job2.Id).Which.HasFieldsWithSameValues(job2);
                Check.That(result).HasElementThatMatches(t => t.Id == job3.Id).Which.HasFieldsWithSameValues(job3);
                Check.That(result).HasElementThatMatches(t => t.Id == job4.Id).Which.HasFieldsWithSameValues(job4);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_MarkAsFinishedAndGetNextJobs_ReturnsNoNextJobs_WhenNoNextJobs(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var job1 = CreateValidJob(conn, "default1", schema, state: EnqueuedState.DefaultName);
                var job2 = CreateValidJob(conn, "default2", schema, state: AwaitingState.DefaultName);
                var job3 = CreateValidJob(conn, "default3", schema, state: AwaitingState.DefaultName);
                var job4 = CreateValidJob(conn, "default4", schema, state: AwaitingState.DefaultName);
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job2.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job3.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job4.Id,
                    ParentJobId = job1.Id
                });
                var result = queryService.MarkAsFinishedAndGetNextJobs(job4.Id, conn);
                Check.That(result).IsEmpty();
            });
        }

        #endregion

        #region GetDependentWorkflowTree

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_GetDependentWorkflowTree_ReturnsAllDependentJobs(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var job1 = CreateValidJob(conn, "default1", schema, state: EnqueuedState.DefaultName);
                var job2 = CreateValidJob(conn, "default2", schema, state: AwaitingState.DefaultName);
                var job3 = CreateValidJob(conn, "default3", schema, state: AwaitingState.DefaultName);
                var job4 = CreateValidJob(conn, "default4", schema, state: AwaitingState.DefaultName);
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job2.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job3.Id,
                    ParentJobId = job2.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job4.Id,
                    ParentJobId = job3.Id
                });
                var result = queryService.GetDependentWorkflowTree(job1.Id, conn);
                Check.That(result).HasElementThatMatches(t => t.Id == job2.Id).Which.HasFieldsWithSameValues(job2);
                Check.That(result).HasElementThatMatches(t => t.Id == job3.Id).Which.HasFieldsWithSameValues(job3);
                Check.That(result).HasElementThatMatches(t => t.Id == job4.Id).Which.HasFieldsWithSameValues(job4);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_GetDependentWorkflowTree_ReturnsNextJobs(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var job1 = CreateValidJob(conn, "default1", schema, state: EnqueuedState.DefaultName);
                var job2 = CreateValidJob(conn, "default2", schema, state: AwaitingState.DefaultName);
                var job3 = CreateValidJob(conn, "default3", schema, state: AwaitingState.DefaultName);
                var job4 = CreateValidJob(conn, "default4", schema, state: AwaitingState.DefaultName);
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job2.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job3.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job4.Id,
                    ParentJobId = job1.Id
                });
                var result = queryService.GetDependentWorkflowTree(job1.Id, conn);
                Check.That(result).HasElementThatMatches(t => t.Id == job2.Id).Which.HasFieldsWithSameValues(job2);
                Check.That(result).HasElementThatMatches(t => t.Id == job3.Id).Which.HasFieldsWithSameValues(job3);
                Check.That(result).HasElementThatMatches(t => t.Id == job4.Id).Which.HasFieldsWithSameValues(job4);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_GetDependentWorkflowTree_ReturnsNoDependencies_WhenNoNextJobs(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var job1 = CreateValidJob(conn, "default1", schema, state: EnqueuedState.DefaultName);
                var job2 = CreateValidJob(conn, "default2", schema, state: AwaitingState.DefaultName);
                var job3 = CreateValidJob(conn, "default3", schema, state: AwaitingState.DefaultName);
                var job4 = CreateValidJob(conn, "default4", schema, state: AwaitingState.DefaultName);
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job2.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job3.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job4.Id,
                    ParentJobId = job1.Id
                });
                var result = queryService.GetDependentWorkflowTree(job4.Id, conn);
                Check.That(result).IsEmpty();
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_GetDependentWorkflowTree_ReturnsNextJobsWithMultipleBranches(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var job1 = CreateValidJob(conn, "default1", schema, state: EnqueuedState.DefaultName);
                var job2 = CreateValidJob(conn, "default2", schema, state: AwaitingState.DefaultName);
                var job3 = CreateValidJob(conn, "default3", schema, state: AwaitingState.DefaultName);
                var job4 = CreateValidJob(conn, "default4", schema, state: AwaitingState.DefaultName);
                var job5 = CreateValidJob(conn, "default5", schema, state: AwaitingState.DefaultName);
                var job6 = CreateValidJob(conn, "default6", schema, state: AwaitingState.DefaultName);
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job2.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job3.Id,
                    ParentJobId = job2.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job4.Id,
                    ParentJobId = job2.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job5.Id,
                    ParentJobId = job4.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job6.Id,
                    ParentJobId = job4.Id
                });
                var result = queryService.GetDependentWorkflowTree(job1.Id, conn);
                Check.That(result).HasElementThatMatches(t => t.Id == job2.Id).Which.HasFieldsWithSameValues(job2);
                Check.That(result).HasElementThatMatches(t => t.Id == job3.Id).Which.HasFieldsWithSameValues(job3);
                Check.That(result).HasElementThatMatches(t => t.Id == job4.Id).Which.HasFieldsWithSameValues(job4);
                Check.That(result).HasElementThatMatches(t => t.Id == job5.Id).Which.HasFieldsWithSameValues(job5);
                Check.That(result).HasElementThatMatches(t => t.Id == job6.Id).Which.HasFieldsWithSameValues(job6);
            });
        }

        [Theory, CleanDatabase("TestSchema", "Pulse")]
        [InlineData("Pulse")]
        [InlineData("TestSchema")]
        public void Run_GetDependentWorkflowTree_ReturnsNextJobsWithMultipleBranches2(string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            UseConnection((conn) =>
            {
                var job1 = CreateValidJob(conn, "default1", schema, state: EnqueuedState.DefaultName);
                var job2 = CreateValidJob(conn, "default2", schema, state: AwaitingState.DefaultName);
                var job3 = CreateValidJob(conn, "default3", schema, state: AwaitingState.DefaultName);
                var job4 = CreateValidJob(conn, "default4", schema, state: AwaitingState.DefaultName);
                var job5 = CreateValidJob(conn, "default5", schema, state: AwaitingState.DefaultName);
                var job6 = CreateValidJob(conn, "default6", schema, state: AwaitingState.DefaultName);
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job2.Id,
                    ParentJobId = job1.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job3.Id,
                    ParentJobId = job2.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job4.Id,
                    ParentJobId = job2.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job5.Id,
                    ParentJobId = job4.Id
                });
                conn.Insert<JobConditionEntity>(new JobConditionEntity()
                {
                    JobId = job6.Id,
                    ParentJobId = job4.Id
                });
                var result = queryService.GetDependentWorkflowTree(job4.Id, conn);
                Check.That(result).HasElementThatMatches(t => t.Id == job5.Id).Which.HasFieldsWithSameValues(job5);
                Check.That(result).HasElementThatMatches(t => t.Id == job6.Id).Which.HasFieldsWithSameValues(job6);
            });
        }

        #endregion

        #region Helpers
        private static void UseConnection(Action<SqlConnection> action)
        {
            using (var connection = ConnectionUtils.GetDatabaseConnection())
            {
                action(connection);
                
            }
        }
        private static T UseConnection<T>(Func<SqlConnection, T> action)
        {
            using (var connection = ConnectionUtils.GetDatabaseConnection())
            {
                return action(connection);
            }
        }
        private JobEntity CreateValidJob(DbConnection conn, string queueName, string schema, DateTime? nextRetry = null, string state = null, string scheduleName = null)
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
                State = state ?? "newState",
                WorkflowId = Guid.NewGuid()
            };
            conn.Insert(job);
            return job;
        }

        private void InsertJobInQueue(DbConnection conn, int jobId, string queueName)
        {
            var queue = new QueueEntity()
            {
                JobId = jobId,
                Queue = queueName,
            };
            conn.Insert(queue);
        }

        private void InsertFetchedJobInQueue(DbConnection conn, int jobId, string queueName, string workerId)
        {
            var queue = new QueueEntity()
            {
                JobId = jobId,
                Queue = queueName,
                WorkerId = workerId
            };
            conn.Insert(queue);
        }

        private string RegisterServerAndWorker(DbConnection conn, string schema)
        {
            var queryService = new QueryService(new SqlServerStorageOptions() { SchemaName = schema });
            var serverId = Guid.NewGuid();
            queryService.HeartbeatServer(serverId.ToString(), "", conn);
            var workerId = Guid.NewGuid();
            queryService.RegisterWorker(workerId.ToString(), serverId.ToString(), conn);
            return workerId.ToString();            
        }

        private ScheduleEntity CreateScheduledEntity(DbConnection conn, bool onlyIfLastFinishedOrFailed = false, DateTime? nextInvocation = null, string scheduleName = null)
        {
            var entity = new ScheduleEntity()
            {
                Cron = "",
                LastInvocation = DateTime.Today,
                Name = scheduleName ?? "dummy",
                OnlyIfLastFinishedOrFailed = onlyIfLastFinishedOrFailed,
                NextInvocation = nextInvocation ?? DateTime.Today
            };
            conn.Insert<ScheduleEntity>(entity);
            return entity;
        }

        #endregion
    }
}
