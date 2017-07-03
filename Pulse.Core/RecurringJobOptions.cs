﻿using Pulse.Core.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public class RecurringJobOptions
    {
        private TimeZoneInfo _timeZone;
        private string _queueName;

        public RecurringJobOptions()
        {
            TimeZone = TimeZoneInfo.Utc;
            QueueName = EnqueuedState.DefaultQueue;
        }
        
        public TimeZoneInfo TimeZone
        {
            get { return _timeZone; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));

                _timeZone = value;
            }
        }
        
        public string QueueName
        {
            get { return _queueName; }
            set
            {
                _queueName = value;
            }
        }

        public int MaxRetries { get; set; } = 10;

        public Guid? ContextId { get; set; }
    }
}
