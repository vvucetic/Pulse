using Dapper;
using Pulse.Core.Log;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public static class SqlServerObjectsInstaller
    {
        public static readonly int RequiredSchemaVersion = 1;
        private const int RetryAttempts = 3;

        private static readonly ILog Log = LogProvider.GetLogger();

        public static void Install(DbConnection connection)
        {
            Install(connection, SqlServerStorageOptions.DefaultSchema);
        }

        public static void Install(DbConnection connection, string schema)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            Log.Log(LogLevel.Information,"Start installing Pulse SQL objects...");

            if (!IsSqlEditionSupported(connection))
            {
                throw new PlatformNotSupportedException("The SQL Server edition of the target server is unsupported.");
            }

            var script = GetStringResource(
                typeof(SqlServerObjectsInstaller).GetTypeInfo().Assembly,
                "Pulse.SqlStorage.Install.sql");

            script = script.Replace("SET @TARGET_SCHEMA_VERSION = 1;", "SET @TARGET_SCHEMA_VERSION = " + RequiredSchemaVersion + ";");

            script = script.Replace("$(PulseSchema)", schema);


            for (var i = 0; i < RetryAttempts; i++)
            {
                try
                {
                    connection.Execute(script);
                    break;
                }
                catch (SqlException ex)
                {
                    if (ex.ErrorCode == 1205)
                    {
                        Log.Log(LogLevel.Information, "Deadlock occurred during automatic migration execution. Retrying...", ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }


            Log.Log(LogLevel.Information, "Pulse SQL objects installed.");
        }

        private static bool IsSqlEditionSupported(DbConnection connection)
        {
            var edition = connection.Query<int>("SELECT SERVERPROPERTY ( 'EngineEdition' )").Single();
            return edition >= SqlEngineEdition.Standard && edition <= SqlEngineEdition.SqlAzure;
        }

        private static string GetStringResource(Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException(
                        $"Requested resource `{resourceName}` was not found in the assembly `{assembly}`.");
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static class SqlEngineEdition
        {
            // ReSharper disable UnusedMember.Local
            // See article http://technet.microsoft.com/en-us/library/ms174396.aspx for details on EngineEdition
            public const int Personal = 1;
            public const int Standard = 2;
            public const int Enterprise = 3;
            public const int Express = 4;
            public const int SqlAzure = 5;
            // ReSharper restore UnusedMember.Local
        }
    }
}
