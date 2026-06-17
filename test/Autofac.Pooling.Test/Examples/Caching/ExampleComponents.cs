// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Autofac.Core;

namespace Autofac.Pooling.Test.Examples.Caching;

/// <summary>
/// A connection-like service that is expensive enough to be worth pooling.
/// </summary>
public interface ICustomConnection
{
    /// <summary>
    /// Gets a stable identifier for the underlying instance, used by the tests
    /// to tell whether the same pooled instance was handed out again.
    /// </summary>
    int InstanceId
    {
        get;
    }

    /// <summary>
    /// Gets the number of times this instance has been taken from the pool.
    /// </summary>
    int GetFromPoolCount
    {
        get;
    }

    /// <summary>
    /// Does some representative work.
    /// </summary>
    /// <returns>A result derived from the work.</returns>
    string DoSomething();
}

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

/// <summary>
/// A component that depends on <see cref="ICustomConnection"/>, used to show that
/// a pooled instance is injected into a consumer like any other dependency.
/// </summary>
public sealed class ConnectionConsumer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionConsumer"/> class.
    /// </summary>
    /// <param name="connection">The pooled connection injected by Autofac.</param>
    public ConnectionConsumer(ICustomConnection connection)
    {
        Connection = connection;
    }

    /// <summary>
    /// Gets the pooled connection that was injected.
    /// </summary>
    public ICustomConnection Connection
    {
        get;
    }
}
