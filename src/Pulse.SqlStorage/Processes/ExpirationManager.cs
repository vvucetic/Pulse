using Pulse.Core.Log;
using Pulse.Core.Server;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.SqlStorage.Processes
{
    internal class ExpirationManager : IBackgroundProcess
    {
        private readonly ILog _logger = LogProvider.GetLogger();

        private const int NumberOfRecordsInSinglePass = 1000;

        private static readonly string[] ProcessedTables =
        {
           "Schedule",
            "Job"
        };

        private readonly SqlStorage _storage;
        private readonly TimeSpan _checkInterval;
        private readonly string _schema;

        public ExpirationManager(SqlStorage storage, TimeSpan checkInterval, string schema)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _checkInterval = checkInterval;
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }
        public void Execute(BackgroundProcessContext context)
        {
            foreach (var table in ProcessedTables)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return;
                _logger.Log($"Removing outdated records from the '{table}' table...");

                _storage.UseConnection((conn) => {
                    try
                    {
                        int affected;

                        do
                        {
                            affected = ExecuteNonQuery(
                                conn,
                                GetQuery(_schema, table),
                                context.CancellationToken,
                                new SqlParameter("@count", NumberOfRecordsInSinglePass),
                                new SqlParameter("@now", DateTime.UtcNow));

                        } while (affected == NumberOfRecordsInSinglePass);
                    }
                    finally
                    {

                    }
                });

                _logger.Log($"Outdated records removed from the '{table}' table.");
            }

            context.CancellationToken.WaitHandle.WaitOne(_checkInterval);
        }

        public override string ToString()
        {
            return GetType().ToString();
        }

        private static string GetQuery(string schemaName, string table)
        {
            return
$@"set transaction isolation level read committed;
delete top (@count) from [{schemaName}].[{table}] with (readpast) 
where ExpireAt < @now";
        }

        private static int ExecuteNonQuery(
            DbConnection connection,
            string commandText,
            CancellationToken cancellationToken,
            params SqlParameter[] parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.Parameters.AddRange(parameters);
                command.CommandTimeout = 0;

                using (cancellationToken.Register(state => ((SqlCommand)state).Cancel(), command))
                {
                    try
                    {
                        return command.ExecuteNonQuery();
                    }
                    catch (SqlException) when (cancellationToken.IsCancellationRequested)
                    {
                        // Exception was triggered due to the Cancel method call, ignoring
                        return 0;
                    }
                }
            }
        }
    }
}
