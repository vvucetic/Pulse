using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.States
{
    public class FailedState : IState
    {
        public FailedState(Exception exception)
        {
            FailedAt = DateTime.UtcNow;
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
        public DateTime FailedAt { get; }
        
        public Exception Exception { get; }

        public static string DefaultName = "Failed";
        public string Name => FailedState.DefaultName;

        public string Reason { get; set; }

        public bool IsFinal => false;

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "FailedAt", JobHelper.SerializeDateTime(FailedAt) },
                { "ExceptionType", Exception.GetType().FullName },
                { "ExceptionMessage", Exception.Message },
                { "ExceptionDetails", Exception.ToString() }
            };
        }
    }
}
