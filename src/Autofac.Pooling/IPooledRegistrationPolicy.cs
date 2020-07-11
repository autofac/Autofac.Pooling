using System;
using System.Collections.Generic;
using Autofac.Core;

namespace Autofac.Pooling
{
    /// <summary>
    /// Defines the interface for a custom pooled registration policy, that defines what happens when an instance is retrieved from the pool, and
    /// what happens when the instance is returned to the pool.
    /// </summary>
    /// <typeparam name="TPooledObject">The type of object being pooled.</typeparam>
    public interface IPooledRegistrationPolicy<TPooledObject>
        where TPooledObject : class
    {
        /// <summary>
        /// Gets a value indicating the maximum number of items that will be retained in the pool.
        /// </summary>
        int MaximumRetained { get; }

        /// <summary>
        /// Invoked when an instance of <typeparamref name="TPooledObject"/> is requested. The policy can invoke <paramref name="getFromPool"/> to
        /// retrieve an instance from the pool. Equally, it could decide to ignore the pool, and just return a custom instance.
        /// </summary>
        /// <param name="context">The current component context.</param>
        /// <param name="parameters">The set of parameters for the resolve request accessing the pool.</param>
        /// <param name="getFromPool">A callback that will retrieve an item from the underlying pool of objects.</param>
        TPooledObject Get(IComponentContext context, IEnumerable<Parameter> parameters, Func<TPooledObject> getFromPool);

        /// <summary>
        /// Invoked when an object is about to be returned into the pool. This method should be used to clean up the state of the object
        /// after being used (resetting connections, releasing temporary resources, etc).
        /// </summary>
        /// <param name="pooledObject">The pooled object.</param>
        /// <returns>
        /// True if the object should be returned to the pool.
        /// False if it should not be placed back in the pool (and will be disposed immediately if it implements <see cref="IDisposable"/>).
        /// </returns>
        bool Return(TPooledObject pooledObject);
    }
}
