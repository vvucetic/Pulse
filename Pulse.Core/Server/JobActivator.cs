using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    public class JobActivator
    {
        private static JobActivator _current = new JobActivator();

        public static JobActivator Current
        {
            get { return _current; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _current = value;
            }
        }


        public virtual object ActivateJob(Type jobType)
        {
            return Activator.CreateInstance(jobType);
        }

        public virtual JobActivatorScope BeginScope()
        {
            return new SimpleJobActivatorScope(this);
        }
      
    }
}
