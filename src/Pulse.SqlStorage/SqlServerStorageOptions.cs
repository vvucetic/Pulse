﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public class SqlServerStorageOptions
    {
        public static readonly string DefaultSchema = "Pulse";
        private TimeSpan _queuePollInterval;
        private string _schemaName;

        public SqlServerStorageOptions()
        {
            QueuePollInterval = TimeSpan.FromSeconds(15);
            InvisibilityTimeout = TimeSpan.FromMinutes(30);
            JobExpirationCheckInterval = TimeSpan.FromMinutes(30);
            DefaultJobExpiration = TimeSpan.FromDays(1);
            TransactionTimeout = TimeSpan.FromMinutes(1);
            PrepareSchemaIfNecessary = true;
            _schemaName = DefaultSchema;
        }
        
        //TODO not used
        public TimeSpan QueuePollInterval
        {
            get { return _queuePollInterval; }
            set
            {
                var message = $"The QueuePollInterval property value should be positive. Given: {value}.";

                if (value == TimeSpan.Zero)
                {
                    throw new ArgumentException(message, nameof(value));
                }
                if (value != value.Duration())
                {
                    throw new ArgumentException(message, nameof(value));
                }

                _queuePollInterval = value;
            }
        }

        //TODO not used
        public TimeSpan InvisibilityTimeout { get; set; }
        
        public bool PrepareSchemaIfNecessary { get; set; }

        /// <summary>
        /// Check interval for Expiration Manager
        /// </summary>
        public TimeSpan JobExpirationCheckInterval { get; set; }

        /// <summary>
        /// Time to expire deleted or expired job
        /// </summary>
        public TimeSpan DefaultJobExpiration { get; set; }

        public TimeSpan TransactionTimeout { get; set; }

        public TimeSpan? CommandTimeout { get; set; }

        public string SchemaName
        {
            get { return _schemaName; }
            set
            {
                if (string.IsNullOrWhiteSpace(_schemaName))
                {
                    throw new ArgumentException(_schemaName, nameof(value));
                }
                _schemaName = value;
            }
        }
    }
}
