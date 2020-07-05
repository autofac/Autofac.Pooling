using Autofac.Core;
using System.Collections.Generic;

namespace Autofac.Pooling.Tests.Types
{
    public class PooledComponent : IPooledService, IPooledComponent
    {
        public int GetCalled { get; private set; }

        public int ReturnCalled { get; private set; }

        public int DisposeCalled { get; private set; }

        public void OnGetFromPool(IComponentContext ctxt, IEnumerable<Parameter> parameters)
        {
            GetCalled++;
        }

        public void OnReturnToPool()
        {
            ReturnCalled++;
        }

        public void Dispose()
        {
            DisposeCalled++;
        }
    }
}
