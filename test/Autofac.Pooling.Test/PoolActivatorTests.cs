// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Autofac.Core;
using Autofac.Pooling.Tests.Common;
using Microsoft.Extensions.ObjectPool;
using Xunit;

namespace Autofac.Pooling.Test;

public class PoolActivatorTests
{
    [Fact]
    public void NullServiceThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PoolActivator<PooledComponent>(
                null!,
                _ => new DefaultPooledRegistrationPolicy<PooledComponent>()));
    }

    [Fact]
    public void NullPolicyFactoryThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PoolActivator<PooledComponent>(
                new UniqueService(),
                null!));
    }

    [Fact]
    public void ProviderFactoryIsOptional()
    {
        // The provider factory may be omitted; the default provider is used.
        using var activator = new PoolActivator<PooledComponent>(
            new UniqueService(),
            _ => new DefaultPooledRegistrationPolicy<PooledComponent>());

        Assert.Equal(typeof(PooledComponent), activator.LimitType);
    }

    [Fact]
    public void ExposesTheLimitType()
    {
        using var activator = new PoolActivator<PooledComponent>(
            new UniqueService(),
            _ => new DefaultPooledRegistrationPolicy<PooledComponent>(),
            _ => new DefaultObjectPoolProvider());

        Assert.Equal(typeof(PooledComponent), activator.LimitType);
    }
}
