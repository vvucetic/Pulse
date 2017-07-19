using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.States
{
    public class SucceededState : IState
    {
        public const string DefaultName = "Succeeded";
        public string Name => SucceededState.DefaultName;

        public string Reason { get; set; }

        public bool IsFinal => true;

        internal SucceededState(object result, long latency, long performanceDuration)
        {
            SucceededAt = DateTime.UtcNow;
            Result = result;
            Latency = latency;
            PerformanceDuration = performanceDuration;

        }
        public DateTime SucceededAt { get; }
        
        public object Result { get; }
        
        public long Latency { get; }
        
        public long PerformanceDuration { get; }

        public Dictionary<string, string> SerializeData()
        {
            var data = new Dictionary<string, string>
            {
                { "SucceededAt",  JobHelper.SerializeDateTime(SucceededAt) },
                { "PerformanceDuration", PerformanceDuration.ToString(CultureInfo.InvariantCulture) },
                { "Latency", Latency.ToString(CultureInfo.InvariantCulture) }
            };

            if (Result != null)
            {
                string serializedResult;

                try
                {
                    serializedResult = JobHelper.ToJson(Result);
                }
                catch (Exception)
                {
                    serializedResult = "Can not serialize the return value";
                }

                if (serializedResult != null)
                {
                    data.Add("Result", serializedResult);
                }
            }

            return data;
        }
    }
}
