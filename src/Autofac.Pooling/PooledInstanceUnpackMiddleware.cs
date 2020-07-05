﻿using System;
using Autofac.Core.Resolving.Pipeline;

namespace Autofac.Pooling
{
    /// <summary>
    /// Middleware for extracting the requested instance from the pool instance container.
    /// </summary>
    /// <typeparam name="TLimit">The component type.</typeparam>
    internal sealed class PooledInstanceUnpackMiddleware<TLimit> : IResolveMiddleware
        where TLimit : class
    {
        /// <inheritdoc/>
        public PipelinePhase Phase => PipelinePhase.Sharing;

        /// <inheritdoc/>
        public void Execute(ResolveRequestContextBase context, Action<ResolveRequestContextBase> next)
        {
            next(context);

            // 'Unpack' the pool instance so what the consumer sees is just the implementation.
            if (context.Instance is PoolInstanceContainer<TLimit> poolInstanceContainer)
            {
                context.Instance = poolInstanceContainer.Instance;
            }
        }
    }
}