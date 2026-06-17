// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Builder;
using Autofac.Pooling.Tests.Common;

namespace Autofac.Pooling.Test;

public class RegistrationExtensionsTests
{
    [Fact]
    public void RequiresCallbackContainer()
    {
        // Manually create a registration builder, then call AsPooled
        var regBuilder = RegistrationBuilder.ForType<PooledComponent>();

        Assert.Throws<NotSupportedException>(() => regBuilder.PooledInstancePerLifetimeScope());
    }

    [Fact]
    public void MatchingScopeWithMaximumRetainedRegistersAndResolves()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerMatchingLifetimeScope(4, "tag");

        var container = builder.Build();

        IPooledService instance;

        using (var scope = container.BeginLifetimeScope("tag"))
        {
            instance = scope.Resolve<IPooledService>();
            Assert.NotNull(instance);
            Assert.Equal(1, instance.GetCalled);
        }

        // Returned to the pool when the matching scope ends.
        Assert.Equal(1, instance.ReturnCalled);

        container.Dispose();
    }

    [Fact]
    public void LifetimeOverriddenAfterPoolingSkipsPoolRegistrations()
    {
        // Changing the lifetime after PooledInstancePerLifetimeScope means the
        // deferred callback should fall back to the original behavior and not
        // wire up the pool. The component then behaves as a plain singleton.
        var builder = new ContainerBuilder();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope()
               .SingleInstance();

        var container = builder.Build();

        var first = container.Resolve<IPooledService>();
        var second = container.Resolve<IPooledService>();

        // SingleInstance won, so it is the same instance and the pool's
        // get-from-pool hook never fired.
        Assert.Same(first, second);
        Assert.Equal(0, first.GetCalled);

        container.Dispose();
    }

    [Fact]
    public void DistinctPooledTypesGetDistinctPools()
    {
        // Exercises PoolService equality/hash-code: two pooled registrations
        // must resolve to independent pools rather than sharing one.
        var builder = new ContainerBuilder();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope();

        builder.RegisterType<OtherPooledComponent>()
               .AsSelf()
               .PooledInstancePerLifetimeScope();

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
        {
            var first = scope.Resolve<IPooledService>();
            var other = scope.Resolve<OtherPooledComponent>();

            Assert.IsType<PooledComponent>(first);
            Assert.NotNull(other);
        }

        container.Dispose();
    }

    [Fact]
    [SuppressMessage("CA2000", "CA2000", Justification = "The container will dispose of the object.")]
    public void NoProvidedInstances()
    {
        var builder = new ContainerBuilder();

        var regBuilder = builder.RegisterInstance(new PooledComponent());

        Assert.Throws<NotSupportedException>(() => regBuilder.PooledInstancePerLifetimeScope());
    }

    [Fact]
    public void OnReleaseNotCompatible()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<PooledComponent>()
               .PooledInstancePerLifetimeScope()
               .OnRelease(args => { });

        Assert.Throws<NotSupportedException>(() => builder.Build());
    }

    [Fact]
    public void OtherLifecycleEventsAreCompatible()
    {
        // A non-OnRelease lifecycle event adds a CoreEventMiddleware of a
        // different event type; pooling must allow it (only OnRelease is
        // rejected).
        var builder = new ContainerBuilder();

        var activated = false;

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope()
               .OnActivated(args => activated = true);

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
        {
            scope.Resolve<IPooledService>();
        }

        Assert.True(activated);

        container.Dispose();
    }
}
