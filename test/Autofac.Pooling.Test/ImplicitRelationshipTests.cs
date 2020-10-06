using Autofac.Features.Metadata;
using Autofac.Features.OwnedInstances;
using Autofac.Pooling.Tests.Shared;
using System;
using System.Collections.Generic;
using Xunit;

namespace Autofac.Pooling.Test
{
    public class ImplicitRelationshipTests
    {
        [Fact]
        public void CanResolveOwnedInstance()
        {
            var builder = new ContainerBuilder();

            var activateCounter = 0;

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope()
                .OnActivated(args => activateCounter++);

            var container = builder.Build();

            using (var scope1 = container.BeginLifetimeScope())
            {
                var owned1 = scope1.Resolve<Owned<IPooledService>>();

                var owned2 = scope1.Resolve<Owned<IPooledService>>();

                // 2 instance per owned should have been activated.
                Assert.Equal(2, activateCounter);

                // Disposing of the owned returns to the pool.
                owned1.Dispose();
                owned2.Dispose();
            }

            container.Dispose();
        }

        [Fact]
        public void CanResolveMetaInstance()
        {
            var builder = new ContainerBuilder();

            var activateCounter = 0;

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope()
                .OnActivated(args => activateCounter++)
                .WithMetadata("key", "value");

            var container = builder.Build();

            var scope1 = container.BeginLifetimeScope();
            var meta1 = scope1.Resolve<Meta<IPooledService>>();

            Assert.Equal("value", meta1.Metadata["key"]);

            var scope2 = container.BeginLifetimeScope();
            var meta2 = scope2.Resolve<Meta<IPooledService>>();

            Assert.Equal("value", meta2.Metadata["key"]);

            Assert.Equal(2, activateCounter);

            scope1.Dispose();
            scope2.Dispose();

            container.Dispose();
        }

        [Fact]
        public void CanResolveLazyInstance()
        {
            var builder = new ContainerBuilder();

            var activateCounter = 0;

            var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(trackingPolicy)
                .OnActivated(args => activateCounter++);

            var container = builder.Build();

            using (var scope1 = container.BeginLifetimeScope())
            {
                var lazy = scope1.Resolve<Lazy<IPooledService>>();

                // Not activated yet.
                Assert.Equal(0, activateCounter);
                Assert.Equal(0, trackingPolicy.OutOfPoolCount);

                // Access the value.
                Assert.NotNull(lazy.Value);

                // Now it's activated.
                Assert.Equal(1, activateCounter);
                Assert.Equal(1, trackingPolicy.OutOfPoolCount);
            }

            // Lifetime scope dispose puts it back in the pool.
            Assert.Equal(0, trackingPolicy.OutOfPoolCount);

            using (var scope2 = container.BeginLifetimeScope())
            {
                var lazy = scope2.Resolve<Lazy<IPooledService>>();

                // Access the value.
                Assert.NotNull(lazy.Value);

                // No increase in activated count, should come from pool.
                Assert.Equal(1, activateCounter);

                // Out of pool count goes up.
                Assert.Equal(1, trackingPolicy.OutOfPoolCount);
            }

            // Lifetime scope dispose puts it back in the pool.
            Assert.Equal(0, trackingPolicy.OutOfPoolCount);

            container.Dispose();
        }

        [Fact]
        public void CanResolveFuncInstance()
        {
            var builder = new ContainerBuilder();

            var activateCounter = 0;

            var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(trackingPolicy)
                .OnActivated(args => activateCounter++);

            var container = builder.Build();

            using (var scope1 = container.BeginLifetimeScope())
            {
                var callback = scope1.Resolve<Func<IPooledService>>();

                // Not activated yet.
                Assert.Equal(0, activateCounter);
                Assert.Equal(0, trackingPolicy.OutOfPoolCount);

                // Access the value.
                Assert.NotNull(callback());

                // Now it's activated.
                Assert.Equal(1, activateCounter);
                Assert.Equal(1, trackingPolicy.OutOfPoolCount);

                // Access the value (will use the shared value).
                Assert.NotNull(callback());

                Assert.Equal(1, trackingPolicy.OutOfPoolCount);
            }

            // Lifetime scope dispose puts it back in the pool.
            Assert.Equal(0, trackingPolicy.OutOfPoolCount);

            container.Dispose();
        }

        [Fact]
        public void CanResolveCollectionOfPooledAndNonPooled()
        {
            var builder = new ContainerBuilder();

            var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(trackingPolicy);
            builder.RegisterType<OtherPooledComponent>().As<IPooledService>().InstancePerLifetimeScope();

            var container = builder.Build();

            using (var scope1 = container.BeginLifetimeScope())
            {
                var set = scope1.Resolve<IList<IPooledService>>();

                Assert.Equal(1, trackingPolicy.OutOfPoolCount);

                Assert.IsType<PooledComponent>(set[0]);
                Assert.IsType<OtherPooledComponent>(set[1]);
            }

            // Lifetime scope dispose puts it back in the pool.
            Assert.Equal(0, trackingPolicy.OutOfPoolCount);

            container.Dispose();
        }

        [Fact]
        public void CanResolveCollectionOfTwoDifferentPoolsOfSameService()
        {
            var builder = new ContainerBuilder();

            var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();
            var otherTrackingPolicy = new PoolTrackingPolicy<OtherPooledComponent>();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(trackingPolicy);
            builder.RegisterType<OtherPooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(otherTrackingPolicy);

            var container = builder.Build();

            using (var scope1 = container.BeginLifetimeScope())
            {
                var set = scope1.Resolve<IList<IPooledService>>();

                Assert.Equal(1, trackingPolicy.OutOfPoolCount);
                Assert.Equal(1, otherTrackingPolicy.OutOfPoolCount);

                Assert.IsType<PooledComponent>(set[0]);
                Assert.IsType<OtherPooledComponent>(set[1]);
            }

            // Lifetime scope dispose puts it back in the pool.
            Assert.Equal(0, trackingPolicy.OutOfPoolCount);
            Assert.Equal(0, otherTrackingPolicy.OutOfPoolCount);

            container.Dispose();
        }

        [Fact]
        public void CanResolveCollectionOfTwoDifferentPoolsOfSameLimitType()
        {
            var builder = new ContainerBuilder();

            var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();
            var otherTrackingPolicy = new PoolTrackingPolicy<PooledComponent>();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(trackingPolicy);
            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(otherTrackingPolicy);

            var container = builder.Build();

            using (var scope1 = container.BeginLifetimeScope())
            {
                var set = scope1.Resolve<IList<IPooledService>>();

                // The important point is that each pool goes up by one, meaning that we can track different pools
                // for the same limit type.
                Assert.Equal(1, trackingPolicy.OutOfPoolCount);
                Assert.Equal(1, otherTrackingPolicy.OutOfPoolCount);
            }

            // Lifetime scope dispose puts it back in the pool.
            Assert.Equal(0, trackingPolicy.OutOfPoolCount);
            Assert.Equal(0, otherTrackingPolicy.OutOfPoolCount);

            container.Dispose();
        }
    }
}
