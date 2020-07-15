using System;
using System.Globalization;
using Autofac.Core;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling
{
    /// <summary>
    /// Defines a service used to access an <see cref="ObjectPool{T}"/>, ensuring that each pooled item registration gets a different pool.
    /// </summary>
    internal class PoolService : Service, IEquatable<PoolService>
    {
        private readonly IComponentRegistration _pooledItemRegistration;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolService"/> class.
        /// </summary>
        /// <param name="pooledItemRegistration">The registration for the pooled item.</param>
        public PoolService(IComponentRegistration pooledItemRegistration)
        {
            _pooledItemRegistration = pooledItemRegistration;
        }

        /// <inheritdoc/>
        public override string Description => string.Format(
            CultureInfo.CurrentCulture,
            PoolServiceResources.Description,
            _pooledItemRegistration.Activator.LimitType.FullName);

        /// <inheritdoc/>
        public bool Equals(PoolService? other)
        {
            return other != null && _pooledItemRegistration.Id == other._pooledItemRegistration.Id;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as PoolService);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _pooledItemRegistration.Id.GetHashCode();
        }
    }
}
