using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Server
{
    class SimpleJobActivatorScope : JobActivatorScope
    {
        private readonly JobActivator _activator;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public SimpleJobActivatorScope(JobActivator activator)
        {
            if (activator == null) throw new ArgumentNullException(nameof(activator));
            _activator = activator;
        }

        public override object Resolve(Type type)
        {
            var instance = _activator.ActivateJob(type);
            var disposable = instance as IDisposable;

            if (disposable != null)
            {
                _disposables.Add(disposable);
            }

            return instance;
        }

        public override void DisposeScope()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
