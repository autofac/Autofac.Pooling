using System;
using System.Collections.Generic;
using Autofac.Core;

namespace Autofac.Pooling
{
    /// <summary>
    /// Provides the default <see cref="IPooledRegistrationPolicy{TLimit}"/>.
    /// </summary>
    /// <typeparam name="TLimit">The limit type.</typeparam>
    public class DefaultPooledRegistrationPolicy<TLimit> : IPooledRegistrationPolicy<TLimit>
        where TLimit : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPooledRegistrationPolicy{TLimit}"/> class.
        /// </summary>
        public DefaultPooledRegistrationPolicy()
        {
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
        public virtual void BeforeGetFromPool(IComponentContext ctxt, IEnumerable<Parameter> parameters)
        {
        }

        /// <inheritdoc/>
        public virtual void AfterGetFromPool(IComponentContext ctxt, IEnumerable<Parameter> parameters, TLimit pooledObject)
        {
        }

        /// <inheritdoc/>
        public virtual bool BeforeReturn(TLimit pooledObject)
        {
            return true;
        }
    }
}
