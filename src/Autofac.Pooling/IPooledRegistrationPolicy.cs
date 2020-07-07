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
        /// Invoked prior to an instance being resolved from the pool. This method can be used to block reads from the pool.
        /// </summary>
        /// <param name="context">The current component context.</param>
        /// <param name="parameters">The set of parameters for the resolve request accessing the pool.</param>
        void BeforeGetFromPool(IComponentContext context, IEnumerable<Parameter> parameters);

        /// <summary>
        /// Invoked after an instance has been retrieved from the pool. This method can be used to provide the pooled object with any
        /// dependencies local to the current scope.
        /// </summary>
        /// <param name="context">The current component context.</param>
        /// <param name="parameters">The set of parameters for the resolve request accessing the pool.</param>
        /// <param name="pooledObject">The object returned from the pool.</param>
        void AfterGetFromPool(IComponentContext context, IEnumerable<Parameter> parameters, TPooledObject pooledObject);

        /// <summary>
        /// Invoked when an object is about to be returned into the pool. This method should be used to clean up the state of the object
        /// after being used (resetting connections, releasing temporary resources, etc).
        /// </summary>
        /// <param name="pooledObject">The pooled object.</param>
        /// <returns>
        /// True if the object should be returned to the pool.
        /// False if it should not be placed back in the pool (and will be disposed when the current scope ends).
        /// </returns>
        bool BeforeReturn(TPooledObject pooledObject);
    }
}
