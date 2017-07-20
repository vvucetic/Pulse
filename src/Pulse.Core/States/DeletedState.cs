using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.States
{
    public class DeletedState : IState
    {
        public DeletedState()
        {
            DeletedAt = DateTime.UtcNow;
        }

        public const string DefaultName = "Deleted";
        public string Name => DeletedState.DefaultName;

        public string Reason { get; set; }

        public bool IsFinal => true;

        public DateTime DeletedAt { get; set; }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "DeletedAt", JobHelper.SerializeDateTime(DeletedAt) }
            };
        }
    }
}
