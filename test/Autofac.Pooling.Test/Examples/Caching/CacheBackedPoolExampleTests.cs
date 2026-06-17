// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Autofac.Pooling.Test.Examples.Caching;

/// <summary>
/// End-to-end example showing how to back a pooled registration with a custom
/// <see cref="Microsoft.Extensions.ObjectPool.ObjectPoolProvider"/> that draws
/// its dependencies (an <see cref="IMemoryCache"/>) from Autofac. Mirrors the
/// "custom pool provider" sample in the README.
/// </summary>
public class CacheBackedPoolExampleTests
{
    [SuppressMessage("CA2000", "CA2000", Justification = "The cache is owned and disposed by the container.")]
    private static IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();

        // Register the backing cache in Autofac. Because it is registered here,
        // the container owns it and disposes it at shutdown, and it can be shared
        // with the rest of the application.
        builder.RegisterInstance(new MemoryCache(new MemoryCacheOptions()))
               .As<IMemoryCache>();

        // Register the provider itself so Autofac constructs it and injects the
        // cache.
        builder.RegisterType<CacheObjectPoolProvider>()
               .SingleInstance();

        // Pool the connection, using the cache-backed provider resolved from the
        // container. The provider factory receives the IComponentContext, so it
        // can resolve the provider - and its dependencies - straight from
        // Autofac.
        builder.RegisterType<MyCustomConnection>()
               .As<ICustomConnection>()
               .PooledInstancePerLifetimeScope(ctx => ctx.Resolve<CacheObjectPoolProvider>());

        // A plain consumer that receives a pooled connection by injection.
        builder.RegisterType<ConnectionConsumer>();

        return builder.Build();
    }

    [Fact]
    public void ConsumerReceivesAPooledConnection()
    {
        using var container = BuildContainer();

        using var scope = container.BeginLifetimeScope();

        // The consumer is injected with a pooled connection like any other
        // dependency; nothing about the registration leaks into the consumer.
        var consumer = scope.Resolve<ConnectionConsumer>();

        Assert.NotNull(consumer.Connection);
        Assert.Equal("connection-" + consumer.Connection.InstanceId, consumer.Connection.DoSomething());

        // The instance came out of the pool, so the get-from-pool hook fired.
        Assert.Equal(1, consumer.Connection.GetFromPoolCount);
    }

    [Fact]
    public void PooledInstanceIsReusedAcrossScopes()
    {
        using var container = BuildContainer();

        int firstInstanceId;

        // First scope: a connection is created and, when the scope ends, returned
        // to the cache-backed pool.
        using (var scope = container.BeginLifetimeScope())
        {
            var connection = scope.Resolve<ICustomConnection>();
            firstInstanceId = connection.InstanceId;
            Assert.Equal(1, connection.GetFromPoolCount);
        }

        // Second scope: the same instance is fetched back from the cache rather
        // than a new one being constructed, and the get-from-pool hook fires
        // again on the reused instance.
        using (var scope = container.BeginLifetimeScope())
        {
            var connection = scope.Resolve<ICustomConnection>();
            Assert.Equal(firstInstanceId, connection.InstanceId);
            Assert.Equal(2, connection.GetFromPoolCount);
        }
    }
}
