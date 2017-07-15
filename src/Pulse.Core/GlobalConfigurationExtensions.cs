using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public static class GlobalConfigurationExtensions
    {
        public static IGlobalConfiguration<TStorage> UseStorage<TStorage>( this IGlobalConfiguration configuration, TStorage storage)
            where TStorage : DataStorage
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            return configuration.Use(storage, x => DataStorage.Current = x);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IGlobalConfiguration<T> Use<T>(this IGlobalConfiguration configuration, T entry,
            Action<T> entryAction)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            entryAction(entry);

            return new ConfigurationEntry<T>(entry);
        }

        private class ConfigurationEntry<T> : IGlobalConfiguration<T>
        {
            public ConfigurationEntry(T entry)
            {
                Entry = entry;
            }

            public T Entry { get; }
        }
    }
}
