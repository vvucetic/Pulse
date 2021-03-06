﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Monitoring.DataModel
{
    public class FailedJobDto
    {
        public JobDto JobInfo { get; set; }

        public string Reason { get; set; }

        public DateTime? FailedAt { get; set; }

        public string ExceptionType { get; set; }

        public string ExceptionMessage { get; set; }

        public string ExceptionDetails { get; set; }
    }
}
