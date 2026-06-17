// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;

namespace Autofac.Pooling.Test.Examples.Caching;

/// <summary>
/// A pooled implementation of <see cref="ICustomConnection"/> that records its
/// pool lifecycle so the tests can observe reuse.
/// </summary>
/// <remarks>
/// Implementing <see cref="IPooledComponent"/> lets the component react when it
/// is taken from or returned to the pool - the natural place to reset
/// per-use state on a reused instance.
/// </remarks>
public sealed class MyCustomConnection : ICustomConnection, IPooledComponent
{
    private static int _instanceCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MyCustomConnection"/> class.
    /// </summary>
    public MyCustomConnection()
    {
        InstanceId = Interlocked.Increment(ref _instanceCounter);
    }

    /// <inheritdoc/>
    public int InstanceId
    {
        get;
    }

    /// <inheritdoc/>
    public int GetFromPoolCount
    {
        get; private set;
    }

    /// <inheritdoc/>
    public string DoSomething() => $"connection-{InstanceId}";

    /// <inheritdoc/>
    public void OnGetFromPool(IComponentContext context, IEnumerable<Parameter> parameters)
    {
        GetFromPoolCount++;
    }

    /// <inheritdoc/>
    public void OnReturnToPool()
    {
        // A real connection would reset per-use state here (clear buffers,
        // roll back transactions, and so on) before being reused.
    }
}
