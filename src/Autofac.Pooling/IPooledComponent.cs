// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Autofac.Core;

namespace Autofac.Pooling
{
    /// <summary>
    /// Components implementing this interface are aware that they are pooled, so will be notified when they are retrieved from the pool,
    /// and when they are returned to the pool.
    /// </summary>
    public interface IPooledComponent
    {
        /// <summary>
        /// Invoked when this instance is retrieved from the pool. Any dependencies retrieved using the provided <see cref="IComponentContext"/>
        /// MUST be discarded in <see cref="OnReturnToPool"/>, otherwise memory leaks may occur.
        /// </summary>
        /// <param name="context">The component context for the current resolve request.</param>
        /// <param name="parameters">The parameters to the resolve request.</param>
        void OnGetFromPool(IComponentContext context, IEnumerable<Parameter> parameters);

        /// <summary>
        /// <para>
        /// Invoked when this instance is being returned to the pool. Any dependencies retrieved in <see cref="OnGetFromPool"/> should
        /// be discarded in this method.
        /// </para>
        ///
        /// <para>
        /// If the maximum pool capacity has been reached (or a custom <see cref="IPooledRegistrationPolicy{T}"/> decides not to return the instance to the pool),
        /// this method will be invoked, but then the object will not be put back in
        /// the pool (and may be disposed if the component implements <see cref="IDisposable"/>).
        /// </para>
        /// </summary>
        void OnReturnToPool();
    }
}
