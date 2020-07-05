using System;
using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling
{
    /// <summary>
    /// Activator that resolves the object pool and retrieves from that rather than creating a new instance.
    /// </summary>
    /// <typeparam name="TLimit">The instance type.</typeparam>
    internal sealed class PoolGetActivator<TLimit> : IInstanceActivator
        where TLimit : class
    {
        private readonly PoolService _poolService;
        private readonly IPooledRegistrationPolicy<TLimit> _registrationPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolGetActivator{TLimit}"/> class.
        /// </summary>
        /// <param name="poolService">The service used to access the pool.</param>
        /// <param name="registrationPolicy">The registration policy for the pool.</param>
        public PoolGetActivator(PoolService poolService, IPooledRegistrationPolicy<TLimit> registrationPolicy)
        {
            _poolService = poolService;
            _registrationPolicy = registrationPolicy;
        }

        /// <inheritdoc/>
        public Type LimitType { get; } = typeof(TLimit);

        /// <inheritdoc/>
        public void ConfigurePipeline(IComponentRegistryServices componentRegistryServices, IResolvePipelineBuilder pipelineBuilder)
        {
            pipelineBuilder.Use(PipelinePhase.Activation, (ctxt, next) =>
            {
                var pool = (ObjectPool<TLimit>)ctxt.ResolveService(_poolService);

                _registrationPolicy.BeforeGetFromPool(ctxt, ctxt.Parameters);

                var poolItem = pool.Get();

                if (poolItem is IPooledComponent poolAwareComponent)
                {
                    poolAwareComponent.OnGetFromPool(ctxt, ctxt.Parameters);
                }

                // This would conceivably allow a service to be captured into
                // a pooled object, and if they don't clear it on return,
                // that object would get left in the instance when it goes back in the pool,
                // and would not be garbage-collected.
                // Documentation necessary to emphasise how important it is to release resources.
                // Implementation of IPooledComponent makes this easier.
                _registrationPolicy.AfterGetFromPool(ctxt, ctxt.Parameters, poolItem);

                // Need to return a 'container' that
                // gets unpacked just after we're done sharing.
                // That way disposal of the scope will return to the pool.
                ctxt.Instance = new PoolInstanceContainer<TLimit>(pool, poolItem);
            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
