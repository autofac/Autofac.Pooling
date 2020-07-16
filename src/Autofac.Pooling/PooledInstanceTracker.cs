// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling
{
    /// <summary>
    /// Tracking class that that will return the wrapped instance to the provided pool when it is disposed.
    /// </summary>
    /// <typeparam name="TPooledObject">The type of the object being pooled.</typeparam>
    internal sealed class PooledInstanceTracker<TPooledObject> : IDisposable
        where TPooledObject : class
    {
        private readonly ObjectPool<TPooledObject> _pool;

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledInstanceTracker{TLimit}"/> class.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="instance">The instance to wrap.</param>
        public PooledInstanceTracker(ObjectPool<TPooledObject> pool, TPooledObject instance)
        {
            _pool = pool;
            Instance = instance;
        }

        /// <summary>
        /// Gets the wrapped instance.
        /// </summary>
        public TPooledObject Instance { get; }

        /// <summary>
        /// Disposes the container, returning the instance to the pool.
        /// </summary>
        public void Dispose()
        {
            // Put the instance back in the pool.
            _pool.Return(Instance);
        }
    }
}
