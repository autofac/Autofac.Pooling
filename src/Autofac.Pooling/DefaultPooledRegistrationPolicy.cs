// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Autofac.Core;

namespace Autofac.Pooling
{
    /// <summary>
    /// Provides the default <see cref="IPooledRegistrationPolicy{TPooledObject}"/>.
    /// </summary>
    /// <typeparam name="TPooledObject">The type of object being pooled.</typeparam>
    public class DefaultPooledRegistrationPolicy<TPooledObject> : IPooledRegistrationPolicy<TPooledObject>
        where TPooledObject : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPooledRegistrationPolicy{TLimit}"/> class.
        /// </summary>
        public DefaultPooledRegistrationPolicy()
        {
            // Based on the default ObjectPool size in the .NET Runtime.
            MaximumRetained = Environment.ProcessorCount * 2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPooledRegistrationPolicy{TLimit}"/> class.
        /// </summary>
        /// <param name="maximumRetained">The maximum number of instances that should be retained in the pool.</param>
        public DefaultPooledRegistrationPolicy(int maximumRetained)
        {
            if (maximumRetained < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumRetained));
            }

            MaximumRetained = maximumRetained;
        }

        /// <inheritdoc/>
        public int MaximumRetained { get; }

        /// <inheritdoc/>
        public virtual TPooledObject Get(IComponentContext context, IEnumerable<Parameter> parameters, Func<TPooledObject> getFromPool)
        {
            if (getFromPool is null)
            {
                throw new ArgumentNullException(nameof(getFromPool));
            }

            return getFromPool();
        }

        /// <inheritdoc/>
        public virtual bool Return(TPooledObject pooledObject)
        {
            return true;
        }
    }
}
