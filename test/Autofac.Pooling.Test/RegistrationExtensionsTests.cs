using System;
using Autofac.Builder;
using Autofac.Pooling.Tests.Common;
using Xunit;

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
}
