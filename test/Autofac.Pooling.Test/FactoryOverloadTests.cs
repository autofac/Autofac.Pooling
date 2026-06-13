// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Autofac.Pooling.Tests.Common;
using Microsoft.Extensions.ObjectPool;
using Xunit;

namespace Autofac.Pooling.Test;

public class FactoryOverloadTests
{
    /// <summary>
    /// A simple tracking pool that records how many instances it has created.
    /// </summary>
    private class TrackingObjectPool<T> : ObjectPool<T>
        where T : class, new()
    {
        private readonly List<T> _items = new();
        private readonly object _lock = new();

        public int CreateCount { get; private set; }

        public override T Get()
        {
            lock (_lock)
            {
                if (_items.Count > 0)
                {
                    var item = _items[^1];
                    _items.RemoveAt(_items.Count - 1);
                    return item;
                }
            }

            CreateCount++;
            return new T();
        }

        public override void Return(T obj)
        {
            lock (_lock)
            {
                _items.Add(obj);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _items.Clear();
            }
        }
    }

    [Fact]
    public void FactoryOverload_RegistersAndResolves()
    {
        var builder = new ContainerBuilder();

        // Register the pool as a component.
        var trackingPool = new TrackingObjectPool<PooledComponent>();
        builder.RegisterInstance(trackingPool).As<ObjectPool<PooledComponent>>();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(sp => sp.Resolve<ObjectPool<PooledComponent>>());

        var container = builder.Build();

        using (var scope1 = container.BeginLifetimeScope())
        {
            scope1.Resolve<IPooledService>();
        }

        // Second scope reuses the instance from the pool.
        using (var scope2 = container.BeginLifetimeScope())
        {
            scope2.Resolve<IPooledService>();
        }

        // Our custom pool was used, not the default one.
        Assert.Equal(1, trackingPool.CreateCount);

        container.Dispose();
    }

    [Fact]
    public void FactoryOverload_ResolveUsesProvidedPool()
    {
        var builder = new ContainerBuilder();

        var trackingPool = new TrackingObjectPool<PooledComponent>();
        builder.RegisterInstance(trackingPool).As<ObjectPool<PooledComponent>>();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(sp => sp.Resolve<ObjectPool<PooledComponent>>());

        var container = builder.Build();

        using (var scope1 = container.BeginLifetimeScope())
        {
            var instance = scope1.Resolve<IPooledService>();
            Assert.NotNull(instance);
            Assert.IsType<PooledComponent>(instance);
        }

        container.Dispose();
    }

    [Fact]
    public void FactoryOverload_WithIPooledComponent_OnReturnToPoolFires()
    {
        var builder = new ContainerBuilder();

        var trackingPool = new TrackingObjectPool<PooledComponent>();
        builder.RegisterInstance(trackingPool).As<ObjectPool<PooledComponent>>();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(sp => sp.Resolve<ObjectPool<PooledComponent>>());

        var container = builder.Build();

        IPooledService instance;

        using (var scope1 = container.BeginLifetimeScope())
        {
            instance = scope1.Resolve<IPooledService>();
            Assert.Equal(1, instance.GetCalled);
        }

        // OnReturnToPool should have been called when the scope ended.
        Assert.Equal(1, instance.ReturnCalled);

        container.Dispose();
    }

    [Fact]
    public void FactoryOverload_WithIPooledComponent_OnGetFromPoolFires()
    {
        var builder = new ContainerBuilder();

        var trackingPool = new TrackingObjectPool<PooledComponent>();
        builder.RegisterInstance(trackingPool).As<ObjectPool<PooledComponent>>();

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(sp => sp.Resolve<ObjectPool<PooledComponent>>());

        var container = builder.Build();

        using (var scope1 = container.BeginLifetimeScope())
        {
            var instance = scope1.Resolve<IPooledService>();
            Assert.Equal(1, instance.GetCalled);

            // Resolve again in the same scope - should get the same shared instance.
            var instance2 = scope1.Resolve<IPooledService>();
            Assert.Same(instance, instance2);

            // OnGetFromPool should only be called once per scope.
            Assert.Equal(1, instance.GetCalled);
        }

        container.Dispose();
    }

    [Fact]
    public void FactoryOverload_MultipleScopesReusesInstance()
    {
        var builder = new ContainerBuilder();

        var trackingPool = new TrackingObjectPool<PooledComponent>();
        builder.RegisterInstance(trackingPool).As<ObjectPool<PooledComponent>>();

        IPooledService capturedInstance = null!;

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(sp => sp.Resolve<ObjectPool<PooledComponent>>());

        var container = builder.Build();

        // First scope - resolves a new instance.
        using (var scope1 = container.BeginLifetimeScope())
        {
            capturedInstance = scope1.Resolve<IPooledService>();
            Assert.Equal(1, capturedInstance.GetCalled);
        }

        // Instance returned to pool; OnReturnToPool should have fired.
        Assert.Equal(1, capturedInstance.ReturnCalled);

        // Second scope - should get same instance from pool.
        using (var scope2 = container.BeginLifetimeScope())
        {
            var instance2 = scope2.Resolve<IPooledService>();
            Assert.Same(capturedInstance, instance2);

            // OnGetFromPool should fire again on second scope.
            Assert.Equal(2, instance2.GetCalled);
        }

        // OnReturnToPool should fire again on second scope.
        Assert.Equal(2, capturedInstance.ReturnCalled);

        // Still only 1 instance created by the pool.
        Assert.Equal(1, trackingPool.CreateCount);

        container.Dispose();
    }

    [Fact]
    public void FactoryOverload_PoolFactoryReceivesComponentContext()
    {
        var builder = new ContainerBuilder();

        // Register a dependency that the pool factory can resolve.
        var poolSize = new PoolConfig { Size = 42 };
        builder.RegisterInstance(poolSize);

        var factoryCalled = false;

        builder.RegisterType<PooledComponent>()
               .As<IPooledService>()
               .PooledInstancePerLifetimeScope(sp =>
               {
                   factoryCalled = true;
                   var config = sp.Resolve<PoolConfig>();
                   Assert.Equal(42, config.Size);
                   return new DefaultObjectPool<PooledComponent>(new DefaultPooledObjectPolicy<PooledComponent>(), config.Size);
               });

        var container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
        {
            scope.Resolve<IPooledService>();
        }

        Assert.True(factoryCalled);

        container.Dispose();
    }

    private class PoolConfig
    {
        public int Size { get; set; }
    }
}
