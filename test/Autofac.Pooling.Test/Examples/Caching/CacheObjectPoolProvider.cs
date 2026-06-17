// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling.Test.Examples.Caching;

/// <summary>
/// An <see cref="ObjectPoolProvider"/> that backs each pool it creates with a
/// shared <see cref="IMemoryCache"/>, demonstrating how a custom provider can
/// take its dependencies from Autofac.
/// </summary>
/// <remarks>
/// Register this provider as a component so that Autofac constructs it and
/// injects the cache, then point a pooled registration at it with
/// <c>ctx =&gt; ctx.Resolve&lt;CacheObjectPoolProvider&gt;()</c>. The provider only
/// controls where instances are stored and when they are evicted; Autofac still
/// owns construction of the pooled instances through the policy it supplies to
/// <see cref="Create{T}(IPooledObjectPolicy{T})"/>.
/// </remarks>
public sealed class CacheObjectPoolProvider : ObjectPoolProvider
{
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheObjectPoolProvider"/>
    /// class.
    /// </summary>
    /// <param name="cache">
    /// The cache used to store pooled instances. Because the cache is injected
    /// rather than created here, its lifetime is owned by the container and the
    /// same cache is shared across every pool this provider creates.
    /// </param>
    public CacheObjectPoolProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Creates a pool that stores instances of <typeparamref name="T"/> in the
    /// shared cache.
    /// </summary>
    /// <typeparam name="T">The type of object to pool.</typeparam>
    /// <param name="policy">
    /// The policy supplied by Autofac. Its <see cref="IPooledObjectPolicy{T}.Create"/>
    /// resolves a fully injected instance through the container, and its
    /// <see cref="IPooledObjectPolicy{T}.Return"/> decides whether an instance is
    /// fit to be retained.
    /// </param>
    /// <returns>A cache-backed pool for <typeparamref name="T"/>.</returns>
    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
        => new CacheObjectPool<T>(_cache, policy);
}
