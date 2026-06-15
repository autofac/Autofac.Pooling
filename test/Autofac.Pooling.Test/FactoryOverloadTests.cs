// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Pooling.Tests.Common;
using Xunit;

namespace Autofac.Pooling.Test;

public class FactoryOverloadTests
{
    [Fact]
    public void PolicyFactory_RegistersAndResolves()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => new DefaultPooledRegistrationPolicy<PooledComponent>());

        var container = builder.Build();

        using (var scope1 = container.BeginLifetimeScope())
        {
            scope1.Resolve<IPooledService>();
        }

        using (var scope2 = container.BeginLifetimeScope())
        {
            scope2.Resolve<IPooledService>();
        }

        container.Dispose();
    }

    [Fact]
    public void PolicyFactory_ResolveUsesDefaultObjectPool()
    {
        var builder = new ContainerBuilder();

        var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();
        builder.RegisterInstance(trackingPolicy).As<IPooledRegistrationPolicy<PooledComponent>>();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => ctx.Resolve<IPooledRegistrationPolicy<PooledComponent>>());

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
    public void PolicyFactory_MultipleScopesReusesInstance()
    {
        var builder = new ContainerBuilder();

        var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();
        builder.RegisterInstance(trackingPolicy).As<IPooledRegistrationPolicy<PooledComponent>>();

        IPooledService capturedInstance = null!;

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => ctx.Resolve<IPooledRegistrationPolicy<PooledComponent>>());

        var container = builder.Build();

        using (var scope1 = container.BeginLifetimeScope())
        {
            capturedInstance = scope1.Resolve<IPooledService>();
            Assert.Equal(1, capturedInstance.GetCalled);
        }

        Assert.Equal(1, capturedInstance.ReturnCalled);

        using (var scope2 = container.BeginLifetimeScope())
        {
            var instance2 = scope2.Resolve<IPooledService>();
            Assert.Same(capturedInstance, instance2);
            Assert.Equal(2, instance2.GetCalled);
        }

        Assert.Equal(2, capturedInstance.ReturnCalled);

        container.Dispose();
    }

    [Fact]
    public void PolicyFactory_PolicyAcceptsAllReturns()
    {
        var builder = new ContainerBuilder();

        var defaultPolicy = new DefaultPooledRegistrationPolicy<PooledComponent>();
        builder.RegisterInstance(defaultPolicy).As<IPooledRegistrationPolicy<PooledComponent>>();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx => ctx.Resolve<IPooledRegistrationPolicy<PooledComponent>>());

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
        {
            scope.Resolve<IPooledService>();
        }

        using (var scope2 = container.BeginLifetimeScope())
        {
            scope2.Resolve<IPooledService>();
        }

        container.Dispose();
    }

    [Fact]
    public void PolicyFactory_PolicyFactoryReceivesComponentContext()
    {
        var builder = new ContainerBuilder();

        var config = new PolicyConfig { MaxRetained = 16 };
        builder.RegisterInstance(config);

        var factoryCalled = false;

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(ctx =>
               {
                   factoryCalled = true;
                   var cfg = ctx.Resolve<PolicyConfig>();
                   Assert.Equal(16, cfg.MaxRetained);
                   return new DefaultPooledRegistrationPolicy<PooledComponent>(cfg.MaxRetained);
               });

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
        {
            scope.Resolve<IPooledService>();
        }

        Assert.True(factoryCalled);

        container.Dispose();
    }

    [Fact]
    public void PolicyFactory_WithMatchingScopeTag()
    {
        var builder = new ContainerBuilder();

        var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();
        builder.RegisterInstance(trackingPolicy).As<IPooledRegistrationPolicy<PooledComponent>>();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerMatchingLifetimeScope(
                   ctx => ctx.Resolve<IPooledRegistrationPolicy<PooledComponent>>(),
                   "tag");

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope("tag"))
        {
            var instance = scope.Resolve<IPooledService>();
            Assert.NotNull(instance);
        }

        container.Dispose();
    }

    private class PolicyConfig
    {
        public int MaxRetained { get; set; }
    }
}
