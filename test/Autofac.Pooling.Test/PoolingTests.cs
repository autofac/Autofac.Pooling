using Autofac.Pooling.Tests.Shared;
using Xunit;

namespace Autofac.Pooling.Tests
{
    public class PoolingTest
    {
        [Fact]
        public void CanRegisterPooledService()
        {
            var builder = new ContainerBuilder();

            var activateCounter = 0;

            // Register a pooled instance. OnActivated only fires when actual instances of the component are created.
            builder.RegisterType<PooledComponent>().As<IPooledService>()
                                                   .PooledInstancePerLifetimeScope()
                                                   .OnActivated(args => activateCounter++);

            var container = builder.Build();

            using (var scope1 = container.BeginLifetimeScope())
            {
                scope1.Resolve<IPooledService>();
            }

            using (var scope2 = container.BeginLifetimeScope())
            {
                scope2.Resolve<IPooledService>();
            }

            // Only 1 instance should have been activated, because we retrieved from the pool each time.
            Assert.Equal(1, activateCounter);

            // When we dispose of the container, our instances are disposed by the pool.
            container.Dispose();
        }

        [Fact]
        public void CanRegisterPooledServiceWithCustomPolicy()
        {
            var builder = new ContainerBuilder();

            var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();

            builder.RegisterType<PooledComponent>().As<IPooledService>()
                                                   .PooledInstancePerLifetimeScope(trackingPolicy);

            var container = builder.Build();

            using (var scope1 = container.BeginLifetimeScope())
            {
                Assert.Equal(0, trackingPolicy.OutOfPoolCount);

                scope1.Resolve<IPooledService>();

                Assert.Equal(1, trackingPolicy.OutOfPoolCount);
            }

            Assert.Equal(0, trackingPolicy.OutOfPoolCount);

            using (var scope2 = container.BeginLifetimeScope())
            {
                scope2.Resolve<IPooledService>();

                Assert.Equal(1, trackingPolicy.OutOfPoolCount);
            }

            Assert.Equal(0, trackingPolicy.OutOfPoolCount);

            // After we dispose of the container, our instances are disposed.
            container.Dispose();
        }

        [Fact]
        public void MultipleActiveScopesIncreaseSizeOfPool()
        {
            var builder = new ContainerBuilder();

            var activateCounter = 0;

            var trackingPolicy = new PoolTrackingPolicy<PooledComponent>();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(trackingPolicy)
                .OnActivated(args => activateCounter++);

            var container = builder.Build();

            Assert.Equal(0, trackingPolicy.OutOfPoolCount);

            var scope1 = container.BeginLifetimeScope();
            scope1.Resolve<IPooledService>();

            Assert.Equal(1, trackingPolicy.OutOfPoolCount);

            var scope2 = container.BeginLifetimeScope();
            scope2.Resolve<IPooledService>();

            Assert.Equal(2, trackingPolicy.OutOfPoolCount);

            Assert.Equal(2, activateCounter);
            scope1.Dispose();

            Assert.Equal(1, trackingPolicy.OutOfPoolCount);

            scope2.Dispose();

            Assert.Equal(0, trackingPolicy.OutOfPoolCount);

            container.Dispose();
        }
    }
}
