// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling.Tests.Common;

/// <summary>
/// A simple in-memory <see cref="ObjectPoolProvider"/> for tests that records
/// how many pools it has created and hands construction back to the supplied
/// <see cref="IPooledObjectPolicy{T}"/>. A single instance can be shared across
/// multiple registrations to prove provider sharing.
/// </summary>
public class TrackingObjectPoolProvider : ObjectPoolProvider
{
    private int _createCount;

    /// <summary>
    /// Gets the number of times <see cref="Create{T}(IPooledObjectPolicy{T})"/>
    /// has been invoked.
    /// </summary>
    public int CreateCount => _createCount;

    /// <inheritdoc/>
    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(policy);

        Interlocked.Increment(ref _createCount);

        return new TrackingObjectPool<T>(policy);
    }

    /// <summary>
    /// A thread-safe pool that delegates construction and return decisions to
    /// the supplied policy. Disposes instances that the policy declines on
    /// return, and disposes everything it retains when the pool itself is
    /// disposed (honouring the disposal contract).
    /// </summary>
    /// <typeparam name="T">The pooled object type.</typeparam>
    private sealed class TrackingObjectPool<T> : ObjectPool<T>, IDisposable
        where T : class
    {
        private readonly IPooledObjectPolicy<T> _policy;
        private readonly ConcurrentQueue<T> _items = new();
        private readonly object _syncRoot = new();
        private bool _disposed;

        public TrackingObjectPool(IPooledObjectPolicy<T> policy)
        {
            _policy = policy;
        }

        public override T Get()
        {
            if (_items.TryDequeue(out var item))
            {
                return item;
            }

            return _policy.Create();
        }

        public override void Return(T obj)
        {
            if (!_policy.Return(obj))
            {
                // The policy declined the instance; the pool owns disposal of
                // declined instances.
                Dispose(obj);
                return;
            }

            // Guard against a return racing with disposal: once disposed, retain
            // nothing (the queue is already drained) and dispose the instance
            // here so it cannot leak.
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    Dispose(obj);
                    return;
                }

                _items.Enqueue(obj);
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            while (_items.TryDequeue(out var item))
            {
                Dispose(item);
            }
        }

        private static void Dispose(T item)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
