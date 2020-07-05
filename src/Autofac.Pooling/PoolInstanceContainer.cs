using System;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling
{
    /// <summary>
    /// Container class that that will return the wrapped instance to the provided pool when it is disposed.
    /// </summary>
    /// <typeparam name="TLimit">The limit type.</typeparam>
    internal sealed class PoolInstanceContainer<TLimit> : IDisposable
        where TLimit : class
    {
        private readonly ObjectPool<TLimit> _pool;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolInstanceContainer{TLimit}"/> class.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="instance">The instance to wrap.</param>
        public PoolInstanceContainer(ObjectPool<TLimit> pool, TLimit instance)
        {
            _pool = pool;
            Instance = instance;
        }

        /// <summary>
        /// Gets the wrapped instance.
        /// </summary>
        public TLimit Instance { get; }

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
