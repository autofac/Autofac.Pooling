// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling;

/// <summary>
/// An activator that creates the <see cref="ObjectPool{TLimit}"/> backing a
/// pooled registration.
/// </summary>
/// <typeparam name="TLimit">
/// The limit type of the objects in the pool.
/// </typeparam>
internal sealed class PoolActivator<TLimit> : IInstanceActivator
    where TLimit : class
{
    private readonly Service _pooledInstanceService;
    private readonly Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> _policyFactory;
    private readonly Func<IComponentContext, ObjectPoolProvider>? _providerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolActivator{TLimit}"/>
    /// class.
    /// </summary>
    /// <param name="pooledInstanceService">
    /// The service used to resolve new instances of the pooled registration.
    /// </param>
    /// <param name="policyFactory">
    /// A factory that returns the policy to use, invoked when the pool is
    /// built.
    /// </param>
    /// <param name="providerFactory">
    /// An optional factory that returns the <see cref="ObjectPoolProvider"/>
    /// that creates the backing pool, invoked when the pool is built. When
    /// <see langword="null"/>, the default <see cref="DefaultObjectPoolProvider"/>
    /// is used, sized from
    /// <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/>.
    /// </param>
    /// <remarks>
    /// Both factories are invoked once during resolve, so the
    /// <see cref="IComponentContext"/> is available for resolving dependencies.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pooledInstanceService"/> or
    /// <paramref name="policyFactory"/> is <see langword="null"/>.
    /// </exception>
    public PoolActivator(
        Service pooledInstanceService,
        Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> policyFactory,
        Func<IComponentContext, ObjectPoolProvider>? providerFactory = null)
    {
        _pooledInstanceService = pooledInstanceService ?? throw new ArgumentNullException(nameof(pooledInstanceService));
        _policyFactory = policyFactory ?? throw new ArgumentNullException(nameof(policyFactory));
        _providerFactory = providerFactory;
    }

    /// <inheritdoc/>
    public Type LimitType { get; } = typeof(TLimit);

    /// <inheritdoc/>
    public void ConfigurePipeline(IComponentRegistryServices componentRegistryServices, IResolvePipelineBuilder pipelineBuilder)
    {
        pipelineBuilder.Use(PipelinePhase.Activation, (context, next) =>
        {
            var policy = _policyFactory(context);
            var scope = context.Resolve<ILifetimeScope>();
            var poolPolicy = new AutofacPooledObjectPolicy<TLimit>(_pooledInstanceService, scope, policy);

            // Use the caller's provider when one was supplied; otherwise the
            // default provider sized from the policy. A custom provider owns
            // sizing and eviction, so MaximumRetained is intentionally consulted
            // only on the default path. The default provider produces a
            // disposable pool when TLimit implements IDisposable.
            var provider = _providerFactory?.Invoke(context)
                ?? new DefaultObjectPoolProvider { MaximumRetained = policy.MaximumRetained };

            var pool = provider.Create(poolPolicy);

            context.Instance = new PooledInstanceContext<TLimit>(pool, policy);
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
