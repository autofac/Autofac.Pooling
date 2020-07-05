using Autofac.Core;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling
{
    /// <summary>
    /// Provides the <see cref="IPooledObjectPolicy{TLimit}"/> needed for creating/returning objects in the pool in an Autofac way.
    /// </summary>
    /// <typeparam name="TLimit">The object pool type.</typeparam>
    internal class AutofacPooledObjectPolicy<TLimit> : IPooledObjectPolicy<TLimit>
        where TLimit : class
    {
        private readonly Service _poolInstanceService;
        private readonly ILifetimeScope _poolOwningScope;
        private readonly IPooledRegistrationPolicy<TLimit> _servicePolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutofacPooledObjectPolicy{TLimit}"/> class.
        /// </summary>
        /// <param name="poolInstanceService">The pooled instance service used to resolve a new instance of the pooled items.</param>
        /// <param name="poolOwningScope">The owning scope of the pool.</param>
        /// <param name="registrationPolicy">The registration policy for the pool.</param>
        public AutofacPooledObjectPolicy(Service poolInstanceService, ILifetimeScope poolOwningScope, IPooledRegistrationPolicy<TLimit> registrationPolicy)
        {
            _poolInstanceService = poolInstanceService;
            _poolOwningScope = poolOwningScope;
            _servicePolicy = registrationPolicy;
        }

        /// <inheritdoc/>
        public TLimit Create()
        {
            return (TLimit)_poolOwningScope.ResolveService(_poolInstanceService);
        }

        /// <inheritdoc/>
        public bool Return(TLimit obj)
        {
            if (_servicePolicy.BeforeReturn(obj))
            {
                if (obj is IPooledComponent poolAwareComponent)
                {
                    poolAwareComponent.OnReturnToPool();
                }

                return true;
            }

            return false;
        }
    }
}
