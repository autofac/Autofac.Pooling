// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;
using Autofac.Pooling.Tests.Common;

namespace Autofac.Pooling.Test;

public class PoolServiceTests
{
    [Fact]
    public void Ctor_DescriptionIncludesTheLimitType()
    {
        var registration = RegistrationFor<PooledComponent>();

        var service = new PoolService(registration);

        Assert.Contains(typeof(PooledComponent).FullName!, service.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Equals_SameUnderlyingRegistration()
    {
        var registration = RegistrationFor<PooledComponent>();

        var first = new PoolService(registration);
        var second = new PoolService(registration);

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentRegistrations()
    {
        var first = new PoolService(RegistrationFor<PooledComponent>());
        var second = new PoolService(RegistrationFor<OtherPooledComponent>());

        Assert.False(first.Equals(second));
    }

    [Fact]
    public void EqualsObject_MatchesAnotherPoolService()
    {
        var registration = RegistrationFor<PooledComponent>();

        var first = new PoolService(registration);
        object second = new PoolService(registration);

        Assert.True(first.Equals(second));
    }

    [Fact]
    public void EqualsObject_UnrelatedType()
    {
        var service = new PoolService(RegistrationFor<PooledComponent>());

        Assert.False(service.Equals("not a pool service"));
    }

    [Fact]
    [SuppressMessage("CA1508", "CA1508", Justification = "Deliberately exercising the Equals null branch for coverage.")]
    public void EqualsObject_Null()
    {
        var service = new PoolService(RegistrationFor<PooledComponent>());

        Assert.False(service.Equals(null));
    }

    private static IComponentRegistration RegistrationFor<TComponent>()
        where TComponent : notnull
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<TComponent>();
        var container = builder.Build();

        return container.ComponentRegistry.Registrations
            .Single(r => r.Activator.LimitType == typeof(TComponent));
    }
}
