// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;

namespace Autofac.Pooling.Tests.Common;

public class PoolTrackingPolicy<TLimit> : DefaultPooledRegistrationPolicy<TLimit>
    where TLimit : class
{
    private int _outOfPool;

    public int OutOfPoolCount => _outOfPool;

    public override TLimit Get(IComponentContext context, IEnumerable<Parameter> parameters, Func<TLimit> getFromPool)
    {
        ArgumentNullException.ThrowIfNull(getFromPool);

        Interlocked.Increment(ref _outOfPool);

        return getFromPool();
    }

    public override bool Return(TLimit pooledObject)
    {
        Interlocked.Decrement(ref _outOfPool);

        return base.Return(pooledObject);
    }
}
