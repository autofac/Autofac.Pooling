// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Pooling.Tests.Common;

namespace Autofac.Pooling.Test;

public class DefaultPooledRegistrationPolicyTests
{
    [Fact]
    public void DefaultConstructorUsesTwiceProcessorCount()
    {
        var policy = new DefaultPooledRegistrationPolicy<PooledComponent>();

        Assert.Equal(Environment.ProcessorCount * 2, policy.MaximumRetained);
    }

    [Fact]
    public void MaximumRetainedConstructorUsesSuppliedValue()
    {
        var policy = new DefaultPooledRegistrationPolicy<PooledComponent>(3);

        Assert.Equal(3, policy.MaximumRetained);
    }

    [Fact]
    public void ZeroMaximumRetainedIsAllowed()
    {
        var policy = new DefaultPooledRegistrationPolicy<PooledComponent>(0);

        Assert.Equal(0, policy.MaximumRetained);
    }

    [Fact]
    public void NegativeMaximumRetainedThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DefaultPooledRegistrationPolicy<PooledComponent>(-1));
    }

    [Fact]
    public void GetInvokesTheGetFromPoolCallback()
    {
        var policy = new DefaultPooledRegistrationPolicy<PooledComponent>();
        using var expected = new PooledComponent();

        var result = policy.Get(null!, Enumerable.Empty<Autofac.Core.Parameter>(), () => expected);

        Assert.Same(expected, result);
    }

    [Fact]
    public void GetWithNullGetFromPoolThrows()
    {
        var policy = new DefaultPooledRegistrationPolicy<PooledComponent>();

        Assert.Throws<ArgumentNullException>(() =>
            policy.Get(null!, Enumerable.Empty<Autofac.Core.Parameter>(), null!));
    }

    [Fact]
    public void ReturnAlwaysAcceptsTheInstance()
    {
        var policy = new DefaultPooledRegistrationPolicy<PooledComponent>();
        using var instance = new PooledComponent();

        Assert.True(policy.Return(instance));
    }
}
