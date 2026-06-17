// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Builder;
using Autofac.Pooling.Tests.Common;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling.Test;

public class RegistrationExtensionsTests
{
    private static IRegistrationBuilder<PooledComponent, ConcreteReflectionActivatorData, SingleRegistrationStyle> NullRegistration
        => null!;

    [Fact]
    public void PooledInstancePerLifetimeScope_RequiresCallbackContainer()
    {
        // Manually create a registration builder, then call AsPooled
        var regBuilder = RegistrationBuilder.ForType<PooledComponent>();

        Assert.Throws<NotSupportedException>(() => regBuilder.PooledInstancePerLifetimeScope());
    }

    [Fact]
    public void PooledInstancePerMatchingLifetimeScope_RegistersAndResolves()
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
    public void PooledInstancePerLifetimeScope_LifetimeOverriddenAfterPoolingSkipsPoolRegistrations()
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
    public void PooledInstancePerLifetimeScope_DistinctPooledTypesGetDistinctPools()
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
    public void PooledInstancePerLifetimeScope_NoProvidedInstances()
    {
        var builder = new ContainerBuilder();

        var regBuilder = builder.RegisterInstance(new PooledComponent());

        Assert.Throws<NotSupportedException>(() => regBuilder.PooledInstancePerLifetimeScope());
    }

    [Fact]
    public void PooledInstancePerLifetimeScope_OnReleaseNotCompatible()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<PooledComponent>()
               .PooledInstancePerLifetimeScope()
               .OnRelease(args => { });

        Assert.Throws<NotSupportedException>(() => builder.Build());
    }

    [Fact]
    public void PooledInstancePerLifetimeScope_OtherLifecycleEventsAreCompatible()
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

    [Fact]
    public void PooledInstancePerLifetimeScope_NullRegistration()
    {
        Assert.Throws<ArgumentNullException>(() => NullRegistration.PooledInstancePerLifetimeScope());
    }

    [Fact]
    public void PooledInstancePerLifetimeScope_NullRegistrationWithMaximumRetained()
    {
        Assert.Throws<ArgumentNullException>(() => NullRegistration.PooledInstancePerLifetimeScope(8));
    }

    [Fact]
    public void PooledInstancePerLifetimeScope_NullRegistrationWithPolicy()
    {
        Assert.Throws<ArgumentNullException>(() => NullRegistration.PooledInstancePerLifetimeScope(new DefaultPooledRegistrationPolicy<PooledComponent>()));
    }

    [Fact]
    public void PooledInstancePerLifetimeScope_NullRegistrationWithPolicyFactory()
    {
        Assert.Throws<ArgumentNullException>(() => NullRegistration.PooledInstancePerLifetimeScope(ctx => new DefaultPooledRegistrationPolicy<PooledComponent>()));
    }

    [Fact]
    public void PooledInstancePerLifetimeScope_NullRegistrationWithProviderFactory()
    {
        Assert.Throws<ArgumentNullException>(() => NullRegistration.PooledInstancePerLifetimeScope(ctx => new DefaultObjectPoolProvider()));
    }

    [Fact]
    public void PooledInstancePerLifetimeScope_NullRegistrationWithPolicyAndProviderFactory()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerLifetimeScope(
                ctx => new DefaultPooledRegistrationPolicy<PooledComponent>(),
                ctx => new DefaultObjectPoolProvider()));
    }

    [Fact]
    public void PooledInstancePerLifetimeScope_NullPolicy()
    {
        var reg = new ContainerBuilder()
            .RegisterType<PooledComponent>()
            .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerLifetimeScope((IPooledRegistrationPolicy<PooledComponent>)null!));
    }

    [Fact]
    public void PooledInstancePerMatchingLifetimeScope_NullRegistration()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope("tag"));
    }

    [Fact]
    public void PooledInstancePerMatchingLifetimeScope_NullRegistrationWithMaximumRetained()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope(8, "tag"));
    }

    [Fact]
    public void PooledInstancePerMatchingLifetimeScope_NullRegistrationWithPolicy()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope(
                new DefaultPooledRegistrationPolicy<PooledComponent>(), "tag"));
    }

    [Fact]
    public void PooledInstancePerMatchingLifetimeScope_NullRegistrationWithPolicyFactory()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope(
                ctx => new DefaultPooledRegistrationPolicy<PooledComponent>(), "tag"));
    }

    [Fact]
    public void PooledInstancePerMatchingLifetimeScope_NullRegistrationWithProviderFactory()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope(
                ctx => new DefaultObjectPoolProvider(), "tag"));
    }

    [Fact]
    public void PooledInstancePerMatchingLifetimeScope_NullRegistrationWithPolicyAndProviderFactory()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope(
                ctx => new DefaultPooledRegistrationPolicy<PooledComponent>(),
                ctx => new DefaultObjectPoolProvider(),
                "tag"));
    }

    [Fact]
    public void PooledInstancePerMatchingLifetimeScope_NullPolicy()
    {
        var reg = new ContainerBuilder()
            .RegisterType<PooledComponent>()
            .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerMatchingLifetimeScope(
                (IPooledRegistrationPolicy<PooledComponent>)null!, "tag"));
    }

    [Fact]
    public void PooledInstancePerMatchingLifetimeScope_NullPolicyFactory()
    {
        var reg = new ContainerBuilder()
            .RegisterType<PooledComponent>()
            .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerMatchingLifetimeScope(
                null!,
                ctx => new DefaultObjectPoolProvider(),
                "tag"));
    }

    [Fact]
    public void PooledInstancePerMatchingLifetimeScope_NullProviderFactory()
    {
        var reg = new ContainerBuilder()
            .RegisterType<PooledComponent>()
            .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerMatchingLifetimeScope(
                ctx => new DefaultPooledRegistrationPolicy<PooledComponent>(),
                null!,
                "tag"));
    }
}
