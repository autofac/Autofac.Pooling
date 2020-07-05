using System;
using System.Collections.Generic;
using Autofac.Core;

namespace Autofac.Pooling
{
    /// <summary>
    /// Components implementing this interface are aware that they are pooled, so will be notified when they are retrieved from the pool,
    /// and when they are returned to the pool.
    /// </summary>
    public interface IPooledComponent : IDisposable
    {
        /// <summary>
        /// Invoked when this instance is retrieved from the pool. Any dependencies retrieved using the provided <see cref="IComponentContext"/>
        /// MUST be discarded in <see cref="OnReturnToPool"/>, otherwise memory leaks may occur.
        /// </summary>
        /// <param name="ctxt">The component context for the current resolve request.</param>
        /// <param name="parameters">The parameters to the resolve request.</param>
        void OnGetFromPool(IComponentContext ctxt, IEnumerable<Parameter> parameters);

        /// <summary>
        /// Invoked when this instance is returned to the pool. Any dependencies retrieved in <see cref="OnGetFromPool"/> should
        /// be discarded in this method.
        /// This method will NOT be invoked if the instance is not returned to the pool
        /// (which can happen if this instance is held past the point of container disposal, or if the ).
        /// The implementation of <see cref="IDisposable"/> should check and potentially discard any resources that <see cref="OnReturnToPool"/> would normally handle.
        /// </summary>
        void OnReturnToPool();
    }
}
