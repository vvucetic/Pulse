using NPoco;
using NPoco.FluentMappings;
using Pulse.SqlStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public static class CustomDatabaseFactory
    {
        public static DatabaseFactory DbFactory { get; set; }

        public static void Setup(string schemaName, string connectionString)
        {
            
            var fluentConfig = NPoco.FluentMappings.FluentMappingConfiguration.Configure(new CustomMappings(schemaName));
            
            DbFactory = DatabaseFactory.Config(x =>
            {
                x.UsingDatabase(() => new Database(connectionString, "System.Data.SqlClient", System.Data.IsolationLevel.ReadCommitted));
                x.WithFluentConfig(fluentConfig);
            });
        }
    }
    public class CustomMappings : Mappings
    {
        public CustomMappings(string schema)
        {
            For<JobConditionEntity>()
                .CompositePrimaryKey(x => x.JobId, x=> x.ParentJobId )
                .TableName($"[{schema}].JobCondition")
                .Columns(x =>
                {
                    x.Column(y => y.Finished);
                    x.Column(y => y.FinishedAt);
                    x.Column(y => y.JobId);
                    x.Column(y => y.ParentJobId);                    
                });
            For<JobEntity>()
                .PrimaryKey(x => x.Id, true)
                .TableName($"[{schema}].Job")
                .Columns(x =>
                {
                    x.Column(y => y.ContextId);
                    x.Column(y => y.CreatedAt);
                    x.Column(y => y.ExpireAt);
                    x.Column(y => y.Id);
                    x.Column(y => y.InvocationData);
                    x.Column(y => y.MaxRetries);
                    x.Column(y => y.NextJobs);
                    x.Column(y => y.NextRetry);
                    x.Column(y => y.Queue);
                    x.Column(y => y.RetryCount);
                    x.Column(y => y.ScheduleName);
                    x.Column(y => y.State);
                    x.Column(y => y.StateId);
                    x.Column(y => y.WorkflowId);
                });
            For<QueueEntity>()
                .PrimaryKey(x => x.Id, true)
                .TableName($"[{schema}].Queue")
                .Columns(x =>
                {
                    x.Column(y => y.FetchedAt);
                    x.Column(y => y.Id);
                    x.Column(y => y.JobId);
                    x.Column(y => y.Queue);
                    x.Column(y => y.WorkerId);
                });
            For<ScheduleEntity>()
                .PrimaryKey(x => x.Name, false)
                .TableName($"[{schema}].Schedule")
                .Columns(x =>
                {
                    x.Column(y => y.Cron);
                    x.Column(y => y.JobInvocationData);
                    x.Column(y => y.LastInvocation);
                    x.Column(y => y.Name);
                    x.Column(y => y.NextInvocation);
                    x.Column(y => y.OnlyIfLastFinishedOrFailed);
                    x.Column(y => y.WorkflowInvocationData);
                });
            For<StateEntity>()
                .PrimaryKey(x => x.Id, true)
                .TableName($"[{schema}].State")
                .Columns(x =>
                {
                    x.Column(y => y.CreatedAt);
                    x.Column(y => y.Data);
                    x.Column(y => y.Id);
                    x.Column(y => y.JobId);
                    x.Column(y => y.Name);
                    x.Column(y => y.Reason);
                });
            For<ServerEntity>()
                .PrimaryKey(x => x.Id, false)
                .TableName($"[{schema}].Server")
                .Columns(x =>
                {
                    x.Column(y => y.Data);
                    x.Column(y => y.Id);
                    x.Column(y => y.LastHeartbeat);
                });
            For<WorkerEntity>()
                .PrimaryKey(x => x.Id, false)
                .TableName($"[{schema}].Worker")
                .Columns(x =>
                {
                    x.Column(y => y.Server);
                    x.Column(y => y.Id);
                });
        }
    }
}
