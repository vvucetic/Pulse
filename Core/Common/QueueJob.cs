using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common
{
    public class QueueJob
    {
        public int JobId { get; set; }

        public string QueueName { get; set; }
        
        public Job Job { get; set; }
    }
}
