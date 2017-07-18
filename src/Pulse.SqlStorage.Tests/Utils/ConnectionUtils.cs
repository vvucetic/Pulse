using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage.Tests.Utils
{
    public static class ConnectionUtils
    {
        private const string DatabaseVariable = "Pulse_SqlServer_DatabaseName";
        private const string ConnectionStringTemplateVariable = "Pulse_SqlServer_ConnectionStringTemplate";

        private const string MasterDatabaseName = "master";
        private const string DefaultDatabaseName = @"Pulse.SqlServer.Tests";
        private const string DefaultConnectionStringTemplate
            = @"Server=.\sqlexpress;Database={0};Trusted_Connection=True;";

        public static string GetDatabaseName()
        {
            return Environment.GetEnvironmentVariable(DatabaseVariable) ?? DefaultDatabaseName;
        }

        public static string GetMasterConnectionString()
        {
            return String.Format(GetConnectionStringTemplate(), MasterDatabaseName);
        }

        public static string GetConnectionString()
        {
            return String.Format(GetConnectionStringTemplate(), GetDatabaseName());
        }

        private static string GetConnectionStringTemplate()
        {
            return Environment.GetEnvironmentVariable(ConnectionStringTemplateVariable)
                   ?? DefaultConnectionStringTemplate;
        }

        public static Database GetDatabaseConnection()
        {
            return new Database(GetConnectionString(), DatabaseType.SqlServer2012, System.Data.IsolationLevel.ReadCommitted, false);
        }

        public static Database GetFactoryDatabaseConnection()
        {
            return CustomDatabaseFactory.DbFactory.GetDatabase();
        }

        public static Database GetMasterDatabaseConnection()
        {
            return new Database(GetMasterConnectionString(), DatabaseType.SqlServer2012, System.Data.IsolationLevel.ReadCommitted, false);
        }
    }

}
