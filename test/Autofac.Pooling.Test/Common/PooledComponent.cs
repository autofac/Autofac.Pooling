using System;
using System.Collections.Generic;
using Autofac.Core;

namespace Autofac.Pooling.Tests.Common;

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

    public void Dispose()
    {
        DisposeCalled++;
    }
}
