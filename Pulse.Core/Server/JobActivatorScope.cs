using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    public abstract class JobActivatorScope : IDisposable
    {
        private static readonly ThreadLocal<JobActivatorScope> _current
            = new ThreadLocal<JobActivatorScope>(trackAllValues: false);

        protected JobActivatorScope()
        {
            _current.Value = this;
        }

        public static JobActivatorScope Current => _current.Value;

        public object InnerScope { get; set; }

        public abstract object Resolve(Type type);

        public virtual void DisposeScope()
        {
        }

        public void Dispose()
        {
            try
            {
                DisposeScope();
            }
            finally
            {
                _current.Value = null;
            }
        }
    }
}
