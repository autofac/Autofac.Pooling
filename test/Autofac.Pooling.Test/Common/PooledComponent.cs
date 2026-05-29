using System;
using System.Collections.Generic;
using Autofac.Core;

namespace Autofac.Pooling.Tests.Common;

[SuppressMessage("CA1063", "CA1063", Justification = "Dispose remains simple here for testing.")]
public class PooledComponent : IPooledService, IPooledComponent, IDisposable
{
    public int GetCalled
    {
        get; private set;
    }

    public int ReturnCalled
    {
        get; private set;
    }

    public int DisposeCalled
    {
        get; private set;
    }

    public void OnGetFromPool(IComponentContext context, IEnumerable<Parameter> parameters)
    {
        GetCalled++;
    }

    public void OnReturnToPool()
    {
        ReturnCalled++;
    }

    [SuppressMessage("CA1063", "CA1063", Justification = "Dispose remains simple here for testing.")]
    public void Dispose()
    {
        DisposeCalled++;
    }
}
