// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;

namespace Autofac.Pooling.Tests.Common;

/// <summary>
/// A pooled component with a constructor dependency, used to prove that construction of pooled
/// instances still flows through the Autofac container when a custom provider is supplied.
/// Tracks pool callbacks so tests can assert the hooks fired.
/// </summary>
public class DependentPooledComponent : IPooledService, IPooledComponent
{
    public DependentPooledComponent(Dependency dependency)
    {
        Dependency = dependency;
    }

    public Dependency Dependency
    {
        get;
    }

    public int GetCalled
    {
        get; private set;
    }

    public int ReturnCalled
    {
        get; private set;
    }

    public int DisposeCalled => 0;

    public void OnGetFromPool(IComponentContext context, IEnumerable<Parameter> parameters)
    {
        GetCalled++;
    }

    public void OnReturnToPool()
    {
        ReturnCalled++;
    }
}
