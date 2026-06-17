// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Globalization;
using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;

namespace Autofac.Pooling;

/// <summary>
/// Activator that resolves the object pool and retrieves from that rather than creating a new instance.
/// </summary>
/// <typeparam name="TLimit">The limit type of the objects in the pool.</typeparam>
internal sealed class PoolGetActivator<TLimit> : IInstanceActivator
    where TLimit : class
{
    private readonly PoolService _poolService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolGetActivator{TLimit}"/> class.
    /// </summary>
    /// <param name="poolService">The service used to access the pool.</param>
    /// <remarks>
    /// The pool and its policy are retrieved from the
    /// <see cref="PooledInstanceContext{TLimit}"/> resolved through
    /// <paramref name="poolService"/> at resolve time.
    /// </remarks>
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
            var ctx = (PooledInstanceContext<TLimit>)ctxt.ResolveService(_poolService);
            var pool = ctx.Pool;
            var policy = ctx.Policy;
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
