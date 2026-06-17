// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling.Test.Examples.Caching;

/// <summary>
/// An <see cref="ObjectPool{T}"/> that stores a single pooled instance of
/// <typeparamref name="T"/> in a shared <see cref="IMemoryCache"/>, letting the
/// cache's expiration policy evict idle instances.
/// </summary>
/// <typeparam name="T">The type of object being pooled.</typeparam>
/// <remarks>
/// <para>
/// This pool does not implement <see cref="IDisposable"/> because it does not
/// own the cache - Autofac created the <see cref="IMemoryCache"/>, so Autofac
/// disposes it. The pool only takes responsibility for the pooled instances
/// themselves: it disposes an instance when the policy declines a return, and
/// registers an eviction callback so the instance is disposed when the cache
/// drops it.
/// </para>
/// <para>
/// The cache is shared across all lifetime scopes and threads, so this pool must
/// be safe for concurrent use; <see cref="IMemoryCache"/> is thread-safe.
/// </para>
/// </remarks>
public sealed class CacheObjectPool<T> : ObjectPool<T>
    where T : class
{
    private readonly IMemoryCache _cache;
    private readonly IPooledObjectPolicy<T> _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheObjectPool{T}"/> class.
    /// </summary>
    /// <param name="cache">The shared cache used to store pooled instances.</param>
    /// <param name="policy">
    /// The policy used to create new instances and to decide whether a returned
    /// instance should be retained.
    /// </param>
    public CacheObjectPool(IMemoryCache cache, IPooledObjectPolicy<T> policy)
    {
        _cache = cache;
        _policy = policy;
    }

    /// <summary>
    /// Gets an instance from the cache, or builds a new one through the policy
    /// (and therefore through Autofac) on a cache miss.
    /// </summary>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    public override T Get()
        => _cache.TryGetValue(typeof(T), out T? item) && item is not null
            ? item
            : _policy.Create();

    /// <summary>
    /// Returns an instance to the pool, storing it in the cache when the policy
    /// accepts it and disposing it otherwise.
    /// </summary>
    /// <param name="obj">The instance being returned.</param>
    public override void Return(T obj)
    {
        // The policy decides whether the instance is fit to be retained.
        if (_policy.Return(obj))
        {
            // Dispose the instance when the cache eventually evicts it; the pool
            // owns disposal of instances the cache drops.
            var options = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) };
            options.RegisterPostEvictionCallback((_, value, _, _) => (value as IDisposable)?.Dispose());
            _cache.Set(typeof(T), obj, options);
        }
        else if (obj is IDisposable disposable)
        {
            // The pool owns disposal of instances it declines.
            disposable.Dispose();
        }
    }
}
