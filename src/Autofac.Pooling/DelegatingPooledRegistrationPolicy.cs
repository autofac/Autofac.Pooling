// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Autofac.Core;

namespace Autofac.Pooling;

/// <summary>
/// A simple <see cref="IPooledRegistrationPolicy{TPooledObject}"/> that delegates
/// <see cref="Get(IComponentContext, IEnumerable{Parameter}, Func{TPooledObject})"/> to the
/// <c>getFromPool</c> callback, allowing the underlying
/// custom <c>ObjectPool&lt;T&gt;</c> to manage its own creation and
/// return behavior. Used when a custom pool is provided via a factory.
/// </summary>
/// <typeparam name="TPooledObject">The type of object being pooled.</typeparam>
internal sealed class DelegatingPooledRegistrationPolicy<TPooledObject> : IPooledRegistrationPolicy<TPooledObject>
    where TPooledObject : class
{
    /// <inheritdoc/>
    public int MaximumRetained => int.MaxValue;

    /// <inheritdoc/>
    public TPooledObject Get(IComponentContext context, IEnumerable<Parameter> parameters, Func<TPooledObject> getFromPool)
    {
        if (getFromPool is null)
        {
            throw new ArgumentNullException(nameof(getFromPool));
        }

        return getFromPool();
    }

    /// <inheritdoc/>
    public bool Return(TPooledObject pooledObject)
    {
        return true;
    }
}
