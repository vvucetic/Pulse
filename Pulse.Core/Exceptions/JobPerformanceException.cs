using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Exceptions
{
    public class JobPerformanceException : Exception
    {
        public JobPerformanceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
