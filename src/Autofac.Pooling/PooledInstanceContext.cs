// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling;

/// <summary>
/// Associates a resolved object pool with the policy used to build it.
/// </summary>
/// <typeparam name="TLimit">
/// The limit type of the objects in the pool.
/// </typeparam>
/// <remarks>
/// The policy is created when the pool is built and shared with the get-side
/// activator through this wrapper, so both sides use the same policy instance.
/// </remarks>
internal sealed class PooledInstanceContext<TLimit> : IDisposable
    where TLimit : class
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PooledInstanceContext{TLimit}"/> class.
    /// </summary>
    /// <param name="pool">
    /// The object pool.
    /// </param>
    /// <param name="policy">
    /// The resolved registration policy.
    /// </param>
    public PooledInstanceContext(ObjectPool<TLimit> pool, IPooledRegistrationPolicy<TLimit> policy)
    {
        Pool = pool;
        Policy = policy;
    }

    /// <summary>
    /// Gets the object pool.
    /// </summary>
    public ObjectPool<TLimit> Pool
    {
        get;
    }

    /// <summary>
    /// Gets the resolved registration policy, shared between pool creation and
    /// retrieval.
    /// </summary>
    public IPooledRegistrationPolicy<TLimit> Policy
    {
        get;
    }

    /// <summary>
    /// Releases the resources used by the wrapper.
    /// </summary>
    /// <remarks>
    /// The container owns this wrapper through the root scope and disposes it
    /// at shutdown. Disposal cascades to <see cref="Pool"/> when the pool
    /// implements <see cref="IDisposable"/>, so the pool can release any
    /// instances it has retained.
    /// </remarks>
    public void Dispose()
    {
        if (Pool is IDisposable disposablePool)
        {
            disposablePool.Dispose();
        }
    }
}
