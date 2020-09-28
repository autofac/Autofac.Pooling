using Autofac.Core;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Autofac.Pooling.Tests.Shared
{
    public class PoolTrackingPolicy<TLimit> : DefaultPooledRegistrationPolicy<TLimit>
        where TLimit : class
    {
        private int _outOfPool;

        public int OutOfPoolCount => _outOfPool;

        public override TLimit Get(IComponentContext context, IEnumerable<Parameter> parameters, Func<TLimit> getFromPool)
        {
            Interlocked.Increment(ref _outOfPool);

            return getFromPool();
        }

        public override bool Return(TLimit pooledObject)
        {
            Interlocked.Decrement(ref _outOfPool);

            return base.Return(pooledObject);
        }
    }
}
