using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Server
{
    public class Worker
    {
        private readonly string _workerId;

        public Worker()
        {
            this._workerId = Guid.NewGuid().ToString();
        }
    }
}
