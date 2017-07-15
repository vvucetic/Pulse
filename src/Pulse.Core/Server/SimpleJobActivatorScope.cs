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
            _activator = activator ?? throw new ArgumentNullException(nameof(activator));
        }

        public override object Resolve(Type type)
        {
            var instance = _activator.ActivateJob(type);

            if (instance is IDisposable disposable)
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
