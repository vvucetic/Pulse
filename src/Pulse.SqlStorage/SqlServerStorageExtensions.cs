using Pulse.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public static class SqlServerStorageExtensions
    {
        public static IGlobalConfiguration<SqlStorage> UseSqlServerStorage(
            this IGlobalConfiguration configuration,
            string nameOrConnectionString)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (nameOrConnectionString == null) throw new ArgumentNullException(nameof(nameOrConnectionString));

            var storage = new SqlStorage(nameOrConnectionString);
            return configuration.UseStorage(storage);
        }

        public static IGlobalConfiguration<SqlStorage> UseSqlServerStorage(
            this IGlobalConfiguration configuration,
            string nameOrConnectionString,
            SqlServerStorageOptions options)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (nameOrConnectionString == null) throw new ArgumentNullException(nameof(nameOrConnectionString));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var storage = new SqlStorage(nameOrConnectionString, options);
            return configuration.UseStorage(storage);
        }
    }
}
