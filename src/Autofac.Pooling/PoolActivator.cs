// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling;

/// <summary>
/// An activator for creating new <see cref="ObjectPool{TLimit}"/> instances.
/// </summary>
/// <typeparam name="TLimit">The limit type of the objects in the pool.</typeparam>
internal sealed class PoolActivator<TLimit> : IInstanceActivator
    where TLimit : class
{
    private readonly Service? _pooledInstanceService;
    private readonly IPooledRegistrationPolicy<TLimit>? _policy;
    private readonly DefaultObjectPoolProvider? _poolProvider;
    private readonly Func<IComponentContext, IPooledRegistrationPolicy<TLimit>>? _policyFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolActivator{TLimit}"/> class
    /// using the default <see cref="DefaultObjectPoolProvider"/>.
    /// </summary>
    /// <param name="pooledInstanceService">The service used to resolve new instances of the pooled registration.</param>
    /// <param name="policy">The pool policy.</param>
    public PoolActivator(Service pooledInstanceService, IPooledRegistrationPolicy<TLimit> policy)
    {
        _pooledInstanceService = pooledInstanceService;
        _policy = policy;
        _poolProvider = new DefaultObjectPoolProvider
        {
            MaximumRetained = policy.MaximumRetained,
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolActivator{TLimit}"/> class
    /// using a factory function to create the <see cref="IPooledRegistrationPolicy{TLimit}"/> at resolve time.
    /// The default <see cref="DefaultObjectPoolProvider"/> will create the backing pool.
    /// </summary>
    /// <param name="pooledInstanceService">The service used to resolve new instances of the pooled registration.</param>
    /// <param name="policyFactory">
    /// A factory that returns the <see cref="IPooledRegistrationPolicy{TLimit}"/> to use.
    /// Invoked during resolve, so the <see cref="IComponentContext"/> is available
    /// for resolving dependencies.
    /// </param>
    public PoolActivator(Service pooledInstanceService, Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> policyFactory)
    {
        _pooledInstanceService = pooledInstanceService;
        _policyFactory = policyFactory ?? throw new ArgumentNullException(nameof(policyFactory));
    }

    /// <inheritdoc/>
    public Type LimitType { get; } = typeof(TLimit);

    /// <inheritdoc/>
    public void ConfigurePipeline(IComponentRegistryServices componentRegistryServices, IResolvePipelineBuilder pipelineBuilder)
    {
        pipelineBuilder.Use(PipelinePhase.Activation, (context, next) =>
        {
            if (_policyFactory is not null)
            {
                // Custom policy factory: resolve the strategy at resolve time, then create DefaultObjectPool.
                var policy = _policyFactory(context);
                var poolProvider = new DefaultObjectPoolProvider
                {
                    MaximumRetained = policy.MaximumRetained,
                };
                var scope = context.Resolve<ILifetimeScope>();
                var poolPolicy = new AutofacPooledObjectPolicy<TLimit>(_pooledInstanceService!, scope, policy);
                var pool = poolProvider.Create(poolPolicy);
                context.Instance = pool;
            }
            else
            {
                // Default path: use DefaultObjectPoolProvider + AutofacPooledObjectPolicy.
                var scope = context.Resolve<ILifetimeScope>();

                var poolPolicy = new AutofacPooledObjectPolicy<TLimit>(_pooledInstanceService!, scope, _policy!);

                // The pool provider will create a disposable pool if the TLimit implements IDisposable.
                var pool = _poolProvider!.Create(poolPolicy);

                context.Instance = pool;
            }
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
