// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Globalization;
using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling;

/// <summary>
/// Activator that resolves the object pool and retrieves from that rather than creating a new instance.
/// </summary>
/// <typeparam name="TLimit">The limit type of the objects in the pool.</typeparam>
internal sealed class PoolGetActivator<TLimit> : IInstanceActivator
    where TLimit : class
{
    private readonly PoolService _poolService;
    private readonly IPooledRegistrationPolicy<TLimit>? _registrationPolicy;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolGetActivator{TLimit}"/> class.
    /// The policy will be retrieved from the <see cref="PooledInstanceContext{TLimit}"/> at resolve time.
    /// </summary>
    /// <param name="poolService">The service used to access the pool.</param>
    public PoolGetActivator(PoolService poolService)
    {
        _poolService = poolService;
    }

    /// <inheritdoc/>
    public Type LimitType { get; } = typeof(TLimit);

    /// <inheritdoc/>
    public void ConfigurePipeline(IComponentRegistryServices componentRegistryServices, IResolvePipelineBuilder pipelineBuilder)
    {
        pipelineBuilder.Use(PipelinePhase.Activation, (ctxt, next) =>
        {
            var resolved = ctxt.ResolveService(_poolService);
            var ctx = resolved as PooledInstanceContext<TLimit>;
            var pool = ctx is not null ? ctx.Pool : (ObjectPool<TLimit>)resolved;
            var policy = ctx is not null ? ctx.Policy : _registrationPolicy!;
            var didGetFromPool = false;

            TLimit PoolGet()
            {
                didGetFromPool = true;
                return pool.Get();
            }

            var poolItem = policy.Get(ctxt, ctxt.Parameters, PoolGet) ?? throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        PoolGetActivatorResources.PolicyMustReturnInstance,
                        policy.GetType().FullName,
                        typeof(TLimit).FullName));

            if (didGetFromPool)
            {
                if (poolItem is IPooledComponent poolAwareComponent)
                {
                    poolAwareComponent.OnGetFromPool(ctxt, ctxt.Parameters);
                }

                // Need to return a 'container' that
                // gets unpacked just after we're done sharing.
                // That way disposal of the scope will return to the pool.
                ctxt.Instance = new PooledInstanceTracker<TLimit>(pool, poolItem);
            }
            else
            {
                // Instance did not come from the pool, so just use it directly.
                ctxt.Instance = poolItem;
            }
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
