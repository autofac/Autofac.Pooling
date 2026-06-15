// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling;

/// <summary>
/// Holds a resolved pool and its associated policy instance,
/// so the policy created during pool construction is shared with the get-side activator.
/// </summary>
/// <typeparam name="TLimit">The limit type of the objects in the pool.</typeparam>
internal sealed class PooledInstanceContext<TLimit>
    where TLimit : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PooledInstanceContext{TLimit}"/> class.
    /// </summary>
    /// <param name="pool">The object pool.</param>
    /// <param name="policy">The resolved registration policy.</param>
    public PooledInstanceContext(ObjectPool<TLimit> pool, IPooledRegistrationPolicy<TLimit> policy)
    {
        Pool = pool;
        Policy = policy;
    }

    /// <summary>
    /// Gets the object pool.
    /// </summary>
    public ObjectPool<TLimit> Pool { get; }

    /// <summary>
    /// Gets the resolved registration policy, shared between pool creation and retrieval.
    /// </summary>
    public IPooledRegistrationPolicy<TLimit> Policy { get; }
}
