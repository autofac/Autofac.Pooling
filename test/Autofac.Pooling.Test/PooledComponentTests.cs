// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Features.OwnedInstances;
using Autofac.Pooling.Tests.Common;

namespace Autofac.Pooling.Test;

public class PooledComponentTests
{
    [Fact]
    public void PooledComponentNotifiedOfGetAndReturn()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope();

        var container = builder.Build();

        var pooled = container.Resolve<Owned<IPooledService>>();

        var pooledInstance = pooled.Value;

        Assert.Equal(1, pooledInstance.GetCalled);

        // Dispose puts it back in the pool.
        pooled.Dispose();

        Assert.Equal(1, pooledInstance.ReturnCalled);
    }

    [Fact]
    public void PooledComponentNotifiedOfGetAndReturnOnlyOnceInLifetimeScope()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope();

        var container = builder.Build();

        IPooledService pooled;

        using (var scope = container.BeginLifetimeScope())
        {
            pooled = scope.Resolve<IPooledService>();

            Assert.Equal(1, pooled.GetCalled);

            var secondPooled = scope.Resolve<IPooledService>();

            Assert.Same(pooled, secondPooled);

            Assert.Equal(1, pooled.GetCalled);
        }

        Assert.Equal(1, pooled.ReturnCalled);
    }
}
