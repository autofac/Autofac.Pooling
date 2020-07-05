using Autofac.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Autofac.Pooling.Tests.Utils
{
    public class PoolTrackingPolicy<TLimit> : DefaultPooledRegistrationPolicy<TLimit>
        where TLimit : class
    {
        private int _outOfPool;

        public int OutOfPoolCount => _outOfPool;

        public override void AfterGetFromPool(IComponentContext ctxt, IEnumerable<Parameter> parameters, TLimit pooledObject)
        {
            Interlocked.Increment(ref _outOfPool);
            base.AfterGetFromPool(ctxt, parameters, pooledObject);
        }

        public override bool BeforeReturn(TLimit pooledObject)
        {
            Interlocked.Decrement(ref _outOfPool);

            return base.BeforeReturn(pooledObject);
        }
    }
}
