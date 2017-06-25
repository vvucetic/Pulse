using Core.Common;
using Core.Exceptions;
using Core.Storage;
using NPoco;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public class SqlStorage : Storage
    {
        private readonly string _connectionStringName;

        //Database _database;

        public SqlStorage(string connectionStringName)
        {
            if (connectionStringName == null) throw new ArgumentNullException(nameof(connectionStringName));
            this._connectionStringName = connectionStringName;
        }
        public override QueueJob FetchNextJob(string queue = "default")
        {
            var fetchJobSqlTemplate = $@"
set transaction isolation level read committed
update top (1) q
set FetchedAt = GETUTCDATE()
output INSERTED.Id as QueueJobId, INSERTED.JobId, INSERTED.Queue
from JobQueue q with (readpast, updlock, rowlock, forceseek)
where Queue = @0 and
(FetchedAt is null or FetchedAt < DATEADD(second, @1, GETUTCDATE()))";
            using (var db = GetDatabase())
            {
                var fetchedJob = db.FirstOrDefault<FetchedJob>(fetchJobSqlTemplate, queue, TimeSpan.FromHours(2).Seconds);
                if (fetchedJob == null)
                    return null;

                var job = db.FirstOrDefault<JobEntity>("SELECT [Id],[State],[InvocationData],[Arguments],[CreatedAt],[NextJobs],[ContextId],[NumberOfConditionJobs] FROM Job WHERE Id = @0", fetchedJob.JobId);
                if (job == null)
                    throw new InternalErrorException("Job not found after fetched from queue.");

                var invocationData = JobHelper.FromJson<InvocationData>(job.InvocationData);
                return new QueueJob()
                {
                    JobId = job.Id,
                    QueueName = fetchedJob.Queue,
                    Job = invocationData.Deserialize()
                };
            }
        }

        public override void Dispose()
        {
            //if (this._database != null)
            //{
            //    this._database.Dispose();
            //}
        }

        private Database GetDatabase(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            return new Database(this._connectionStringName);
        }

        private string GetConnectionString(string nameOrConnectionString)
        {
            if (IsConnectionString(nameOrConnectionString))
            {
                return nameOrConnectionString;
            }

            if (IsConnectionStringInConfiguration(nameOrConnectionString))
            {
                return ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString;
            }

            throw new ArgumentException($"Could not find connection string with name '{nameOrConnectionString}' in application config file");
        }

        private bool IsConnectionString(string nameOrConnectionString)
        {
            return nameOrConnectionString.Contains(";");
        }

        private bool IsConnectionStringInConfiguration(string connectionStringName)
        {
            var connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSetting != null;
        }
    }
}
