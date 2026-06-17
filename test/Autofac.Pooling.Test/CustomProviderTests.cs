// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Pooling.Tests.Common;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling.Test;

public class CustomProviderTests
{
    [Fact]
    public void Provider_RegistersAndResolves()
    {
        var builder = new ContainerBuilder();

        var provider = new TrackingObjectPoolProvider();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => provider);

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
        {
            var instance = scope.Resolve<IPooledService>();
            Assert.NotNull(instance);
            Assert.IsType<PooledComponent>(instance);
        }

        container.Dispose();
    }

    [Fact]
    public void Provider_BuildsThePoolExactlyOnce()
    {
        var builder = new ContainerBuilder();

        var provider = new TrackingObjectPoolProvider();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => provider);

        var container = builder.Build();

        // Resolve across several scopes; the provider should only build one pool.
        for (var i = 0; i < 3; i++)
        {
            using var scope = container.BeginLifetimeScope();
            scope.Resolve<IPooledService>();
        }

        Assert.Equal(1, provider.CreateCount);

        container.Dispose();
    }

    [Fact]
    public void Provider_ConstructionGoesThroughAutofac()
    {
        var builder = new ContainerBuilder();

        var dependency = new Dependency();
        builder.RegisterInstance(dependency);

        builder.RegisterType<DependentPooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => new TrackingObjectPoolProvider());

        var container = builder.Build();

        DependentPooledComponent instance;

        using (var scope = container.BeginLifetimeScope())
        {
            instance = Assert.IsType<DependentPooledComponent>(scope.Resolve<IPooledService>());

            // The dependency was injected by the container, proving construction
            // still flows through Autofac on the custom-provider path.
            Assert.Same(dependency, instance.Dependency);

            // And the pooling hooks still fire on this path.
            Assert.Equal(1, instance.GetCalled);
        }

        Assert.Equal(1, instance.ReturnCalled);

        container.Dispose();
    }

    [Fact]
    public void Provider_PooledComponentHooksFire()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => new TrackingObjectPoolProvider());

        var container = builder.Build();

        IPooledService captured;

        using (var scope = container.BeginLifetimeScope())
        {
            captured = scope.Resolve<IPooledService>();
            Assert.Equal(1, captured.GetCalled);
            Assert.Equal(0, captured.ReturnCalled);
        }

        // Scope disposal returns the instance to the pool, firing OnReturnToPool.
        Assert.Equal(1, captured.ReturnCalled);

        using (var scope = container.BeginLifetimeScope())
        {
            var reused = scope.Resolve<IPooledService>();
            Assert.Same(captured, reused);
            Assert.Equal(2, reused.GetCalled);
        }

        Assert.Equal(2, captured.ReturnCalled);

        container.Dispose();
    }

    [Fact]
    public void Provider_RegistrationPolicyStillApplies()
    {
        var builder = new ContainerBuilder();

        var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();
        var provider = new TrackingObjectPoolProvider();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => trackingPolicy, ctx => provider);

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
        {
            scope.Resolve<IPooledService>();

            // The policy is mid-Get (out of the pool) during the scope.
            Assert.Equal(1, trackingPolicy.OutOfPoolCount);
        }

        // Return brings the count back down.
        Assert.Equal(0, trackingPolicy.OutOfPoolCount);
        Assert.Equal(1, provider.CreateCount);

        container.Dispose();
    }

    [Fact]
    public void Provider_SharedAcrossTypesUsesSameProviderInstance()
    {
        var builder = new ContainerBuilder();

        var sharedProvider = new TrackingObjectPoolProvider();
        builder.RegisterInstance(sharedProvider).As<TrackingObjectPoolProvider>();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => ctx.Resolve<TrackingObjectPoolProvider>());

        builder.RegisterType<OtherPooledComponent>()
               .As<OtherPooledComponent>()
               .PooledInstancePerLifetimeScope(ctx => ctx.Resolve<TrackingObjectPoolProvider>());

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
        {
            scope.Resolve<IPooledService>();
            scope.Resolve<OtherPooledComponent>();
        }

        // One shared provider built both pools (Create<PooledComponent> and Create<OtherPooledComponent>).
        Assert.Equal(2, sharedProvider.CreateCount);

        container.Dispose();
    }

    [Fact]
    public void Provider_PerTypeDifferentProvidersDoNotCrossTalk()
    {
        var builder = new ContainerBuilder();

        var providerA = new TrackingObjectPoolProvider();
        var providerB = new TrackingObjectPoolProvider();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => providerA);

        builder.RegisterType<OtherPooledComponent>()
               .As<OtherPooledComponent>()
               .PooledInstancePerLifetimeScope(ctx => providerB);

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
        {
            scope.Resolve<IPooledService>();
            scope.Resolve<OtherPooledComponent>();
        }

        Assert.Equal(1, providerA.CreateCount);
        Assert.Equal(1, providerB.CreateCount);

        container.Dispose();
    }

    [Fact]
    public void Provider_MatchingScopeVariant()
    {
        var builder = new ContainerBuilder();

        var provider = new TrackingObjectPoolProvider();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerMatchingLifetimeScope(ctx => provider, "tag");

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope("tag"))
        {
            var instance = scope.Resolve<IPooledService>();
            Assert.NotNull(instance);
        }

        Assert.Equal(1, provider.CreateCount);

        container.Dispose();
    }

    [Fact]
    public void Provider_MatchingScopeVariantWithPolicy()
    {
        var builder = new ContainerBuilder();

        var provider = new TrackingObjectPoolProvider();
        var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerMatchingLifetimeScope(ctx => trackingPolicy, ctx => provider, "tag");

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope("tag"))
        {
            scope.Resolve<IPooledService>();
            Assert.Equal(1, trackingPolicy.OutOfPoolCount);
        }

        Assert.Equal(0, trackingPolicy.OutOfPoolCount);
        Assert.Equal(1, provider.CreateCount);

        container.Dispose();
    }

    [Fact]
    public void Provider_DisposableCustomPoolDisposedAtShutdown()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => new TrackingObjectPoolProvider());

        var container = builder.Build();

        IPooledService captured;

        using (var scope = container.BeginLifetimeScope())
        {
            captured = scope.Resolve<IPooledService>();
        }

        // Returned to the (custom, disposable) pool but not disposed yet.
        Assert.Equal(0, captured.DisposeCalled);

        // Disposing the container disposes the wrapper, which cascades to the disposable pool,
        // which disposes the instances it retained.
        container.Dispose();

        Assert.Equal(1, captured.DisposeCalled);
    }

    [Fact]
    public void Provider_NullProviderFactory()
    {
        var builder = new ContainerBuilder();

        var reg = builder.RegisterType<PooledComponent>()
                         .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerLifetimeScope(
                (Func<IComponentContext, ObjectPoolProvider>)null!));
    }

    [Fact]
    public void Provider_NullProviderFactoryWithPolicy()
    {
        var builder = new ContainerBuilder();

        var reg = builder.RegisterType<PooledComponent>()
                         .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerLifetimeScope(
                ctx => new DefaultPooledRegistrationPolicy<PooledComponent>(),
                null!));
    }

    [Fact]
    public void Provider_NullPolicyFactoryWithProvider()
    {
        var builder = new ContainerBuilder();

        var reg = builder.RegisterType<PooledComponent>()
                         .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerLifetimeScope(
                null!,
                ctx => new TrackingObjectPoolProvider()));
    }

    [Fact]
    public void Provider_NullProviderFactoryWithMatchingScope()
    {
        var builder = new ContainerBuilder();

        var reg = builder.RegisterType<PooledComponent>()
                         .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerMatchingLifetimeScope(
                (Func<IComponentContext, ObjectPoolProvider>)null!,
                "tag"));
    }

    [Fact]
    public async Task Provider_CanUseConcurrently()
    {
        var builder = new ContainerBuilder();

        var provider = new TrackingObjectPoolProvider();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => provider);

        var container = builder.Build();

        var exception = await Record.ExceptionAsync(async () =>
        {
            await Task.WhenAll(Enumerable.Range(0, 100).Select(i => Task.Run(() =>
            {
                using var scope = container.BeginLifetimeScope();
                scope.Resolve<IPooledService>();
            })));

            container.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Provider_OverloadBindsToProviderFactory()
    {
        var builder = new ContainerBuilder();

        // Register an ObjectPoolProvider so the lambda's return type is ObjectPoolProvider,
        // which must bind to the provider overload rather than the policy-factory overload.
        builder.RegisterInstance<ObjectPoolProvider>(new TrackingObjectPoolProvider());

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => ctx.Resolve<ObjectPoolProvider>());

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
        {
            Assert.NotNull(scope.Resolve<IPooledService>());
        }

        container.Dispose();
    }
}
