// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Autofac.Builder;
using Autofac.Pooling.Tests.Common;
using Microsoft.Extensions.ObjectPool;
using Xunit;

namespace Autofac.Pooling.Test;

/// <summary>
/// Verifies that every public registration overload null-guards its parameters.
/// </summary>
public class RegistrationExtensionsNullGuardTests
{
    private static IRegistrationBuilder<PooledComponent, ConcreteReflectionActivatorData, SingleRegistrationStyle> NullRegistration
        => null!;

    [Fact]
    public void PerLifetimeScope_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerLifetimeScope());
    }

    [Fact]
    public void PerLifetimeScope_MaximumRetained_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerLifetimeScope(8));
    }

    [Fact]
    public void PerLifetimeScope_Policy_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerLifetimeScope(new DefaultPooledRegistrationPolicy<PooledComponent>()));
    }

    [Fact]
    public void PerLifetimeScope_PolicyFactory_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerLifetimeScope(
                ctx => new DefaultPooledRegistrationPolicy<PooledComponent>()));
    }

    [Fact]
    public void PerLifetimeScope_ProviderFactory_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerLifetimeScope(
                ctx => new DefaultObjectPoolProvider()));
    }

    [Fact]
    public void PerLifetimeScope_PolicyAndProviderFactory_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerLifetimeScope(
                ctx => new DefaultPooledRegistrationPolicy<PooledComponent>(),
                ctx => new DefaultObjectPoolProvider()));
    }

    [Fact]
    public void PerMatchingLifetimeScope_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope("tag"));
    }

    [Fact]
    public void PerMatchingLifetimeScope_MaximumRetained_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope(8, "tag"));
    }

    [Fact]
    public void PerMatchingLifetimeScope_Policy_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope(
                new DefaultPooledRegistrationPolicy<PooledComponent>(), "tag"));
    }

    [Fact]
    public void PerMatchingLifetimeScope_PolicyFactory_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope(
                ctx => new DefaultPooledRegistrationPolicy<PooledComponent>(), "tag"));
    }

    [Fact]
    public void PerMatchingLifetimeScope_ProviderFactory_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope(
                ctx => new DefaultObjectPoolProvider(), "tag"));
    }

    [Fact]
    public void PerMatchingLifetimeScope_PolicyAndProviderFactory_NullRegistrationThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullRegistration.PooledInstancePerMatchingLifetimeScope(
                ctx => new DefaultPooledRegistrationPolicy<PooledComponent>(),
                ctx => new DefaultObjectPoolProvider(),
                "tag"));
    }

    [Fact]
    public void PerLifetimeScope_NullPolicyThrows()
    {
        var reg = new ContainerBuilder()
            .RegisterType<PooledComponent>()
            .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerLifetimeScope((IPooledRegistrationPolicy<PooledComponent>)null!));
    }

    [Fact]
    public void PerMatchingLifetimeScope_NullPolicyThrows()
    {
        var reg = new ContainerBuilder()
            .RegisterType<PooledComponent>()
            .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerMatchingLifetimeScope(
                (IPooledRegistrationPolicy<PooledComponent>)null!, "tag"));
    }

    [Fact]
    public void PerMatchingLifetimeScope_PolicyAndProviderFactory_NullPolicyFactoryThrows()
    {
        var reg = new ContainerBuilder()
            .RegisterType<PooledComponent>()
            .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerMatchingLifetimeScope(
                (Func<IComponentContext, IPooledRegistrationPolicy<PooledComponent>>)null!,
                ctx => new DefaultObjectPoolProvider(),
                "tag"));
    }

    [Fact]
    public void PerMatchingLifetimeScope_PolicyAndProviderFactory_NullProviderFactoryThrows()
    {
        var reg = new ContainerBuilder()
            .RegisterType<PooledComponent>()
            .As<IPooledService>();

        Assert.Throws<ArgumentNullException>(() =>
            reg.PooledInstancePerMatchingLifetimeScope(
                ctx => new DefaultPooledRegistrationPolicy<PooledComponent>(),
                (Func<IComponentContext, ObjectPoolProvider>)null!,
                "tag"));
    }
}
