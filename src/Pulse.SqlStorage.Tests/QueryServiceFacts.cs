using Moq;
using Pulse.SqlStorage.Entities;
using Pulse.SqlStorage.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Pulse.SqlStorage.Tests
{
    public class QueryServiceFacts
    {       

        public QueryServiceFacts()
        {
            CustomDatabaseFactory.Setup(SqlServerStorageOptions.DefaultSchema, ConnectionUtils.GetConnectionString());
        }

        [Fact, CleanDatabase]
        public void Run_GetNonNull_WhenValidJobExists()
        {
            var queryService = new QueryService(new SqlServerStorageOptions());
            CreateValidJob();
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var serverId = Guid.NewGuid();
                queryService.HeartbeatServer(serverId.ToString(), "", db);
                var workerId = Guid.NewGuid();
                queryService.RegisterWorker(workerId.ToString(), serverId.ToString(), db);
                var job = queryService.FetchNextJob(new[] { "default" }, workerId.ToString(), db);
                Assert.NotNull(job);
            }
        }

        private void CreateValidJob()
        {
            using (var db = Utils.ConnectionUtils.GetFactoryDatabaseConnection())
            {
                var queryService = new QueryService(new SqlServerStorageOptions());
                var job = new JobEntity()
                {
                    InvocationData = "{}",
                    NextJobs = "[]"
                };
                db.Insert(job);
                var queue = new QueueEntity()
                {
                    JobId = job.Id,
                    Queue = "default"
                };
                db.Insert(queue);
            }
        }
    }
}
