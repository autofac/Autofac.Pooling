using Autofac.Core;
using Autofac.Pooling.Tests.Types;
using Autofac.Pooling.Tests.Utils;
using Xunit;

namespace Autofac.Pooling.Tests
{
    public class LifetimeScopeTests
    {
        [Fact]
        public void EachNestedScopeGetsOwnInstanceFromPool()
        {
            var builder = new ContainerBuilder();

            var tracking = new PoolTrackingPolicy<PooledComponent>();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(tracking);

            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                var outerInstance = scope.Resolve<IPooledService>();

                Assert.Equal(1, tracking.OutOfPoolCount);

                using (var innerScope = scope.BeginLifetimeScope())
                {
                    var innerInstance = innerScope.Resolve<IPooledService>();

                    Assert.Equal(2, tracking.OutOfPoolCount);

                    Assert.NotSame(outerInstance, innerInstance);
                }

                Assert.Equal(1, tracking.OutOfPoolCount);
            }

            Assert.Equal(0, tracking.OutOfPoolCount);
        }

        [Fact]
        public void MatchingScopeSharesPooledInstanceWithNestedScopes()
        {
            var builder = new ContainerBuilder();

            var tracking = new PoolTrackingPolicy<PooledComponent>();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerMatchingLifetimeScope(tracking, "tag");

            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope("tag"))
            {
                var outerInstance = scope.Resolve<IPooledService>();

                Assert.Equal(1, tracking.OutOfPoolCount);

                using (var innerScope = scope.BeginLifetimeScope())
                {
                    var innerInstance = innerScope.Resolve<IPooledService>();

                    Assert.Equal(1, tracking.OutOfPoolCount);

                    Assert.Same(outerInstance, innerInstance);
                }

                Assert.Equal(1, tracking.OutOfPoolCount);
            }

            Assert.Equal(0, tracking.OutOfPoolCount);
        }

        [Fact]
        public void MatchingScopeNotFoundErrorThrownWithPooledRegistration()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerMatchingLifetimeScope("tag");

            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                Assert.Throws<DependencyResolutionException>(() => scope.Resolve<IPooledService>());
            }
        }

        [Fact]
        public void PoolCanBeOverriddenInNestedScope()
        {
            var builder = new ContainerBuilder();

            var outerPolicy = new PoolTrackingPolicy<PooledComponent>();
            var innerPolicy = new PoolTrackingPolicy<PooledComponent>();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(outerPolicy);

            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                var outerInstance = scope.Resolve<IPooledService>();

                Assert.Equal(1, outerPolicy.OutOfPoolCount);

                using (var overrideScope = scope.BeginLifetimeScope(cfg =>
                {
                    cfg.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(innerPolicy);
                }))
                {
                    var innerInstance = overrideScope.Resolve<IPooledService>();

                    Assert.Equal(1, outerPolicy.OutOfPoolCount);
                    Assert.Equal(1, innerPolicy.OutOfPoolCount);

                    Assert.NotSame(outerInstance, innerInstance);
                }

                Assert.Equal(0, innerPolicy.OutOfPoolCount);
                Assert.Equal(1, outerPolicy.OutOfPoolCount);
            }

            Assert.Equal(0, outerPolicy.OutOfPoolCount);
        }
    }
}
