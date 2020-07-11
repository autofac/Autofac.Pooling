using Autofac.Core;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling
{
    /// <summary>
    /// Provides the <see cref="IPooledObjectPolicy{TLimit}"/> needed for creating/returning objects in the pool in an Autofac way.
    /// </summary>
    /// <typeparam name="TPooledObject">The type of object being pooled.</typeparam>
    internal class AutofacPooledObjectPolicy<TPooledObject> : IPooledObjectPolicy<TPooledObject>
        where TPooledObject : class
    {
        private readonly Service _poolInstanceService;
        private readonly ILifetimeScope _poolOwningScope;
        private readonly IPooledRegistrationPolicy<TPooledObject> _servicePolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutofacPooledObjectPolicy{TLimit}"/> class.
        /// </summary>
        /// <param name="poolInstanceService">The pooled instance service used to resolve a new instance of the pooled items.</param>
        /// <param name="poolOwningScope">The owning scope of the pool.</param>
        /// <param name="registrationPolicy">The registration policy for the pool.</param>
        public AutofacPooledObjectPolicy(Service poolInstanceService, ILifetimeScope poolOwningScope, IPooledRegistrationPolicy<TPooledObject> registrationPolicy)
        {
            _poolInstanceService = poolInstanceService;
            _poolOwningScope = poolOwningScope;
            _servicePolicy = registrationPolicy;
        }

        /// <inheritdoc/>
        public TPooledObject Create()
        {
            return (TPooledObject)_poolOwningScope.ResolveService(_poolInstanceService);
        }

        /// <inheritdoc/>
        public bool Return(TPooledObject obj)
        {
            if (obj is IPooledComponent poolAwareComponent)
            {
                poolAwareComponent.OnReturnToPool();
            }

            if (_servicePolicy.Return(obj))
            {
                return true;
            }

            return false;
        }
    }
}
