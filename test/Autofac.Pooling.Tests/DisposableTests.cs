using Autofac.Features.OwnedInstances;
using Autofac.Pooling.Tests.Shared;
using Xunit;

namespace Autofac.Pooling.Tests
{
    public class DisposableTests
    {
        [Fact]
        public void DisposableRegistrationsAreNotDisposedWhenReturnedToThePool()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope();

            var container = builder.Build();

            var pooled = container.Resolve<Owned<IPooledService>>();

            var pooledInstance = pooled.Value;

            Assert.Equal(0, pooledInstance.DisposeCalled);

            // Dispose puts it back in the pool.
            pooled.Dispose();

            // Returned, but not disposed.
            Assert.Equal(1, pooledInstance.ReturnCalled);
            Assert.Equal(0, pooledInstance.DisposeCalled);
        }

        [Fact]
        public void DisposableRegistrationsDisposedWhenContainerIsDisposed()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope();

            var container = builder.Build();

            var pooled = container.Resolve<Owned<IPooledService>>();

            var pooledInstance = pooled.Value;

            // Dispose puts it back in the pool.
            pooled.Dispose();

            Assert.Equal(0, pooledInstance.DisposeCalled);

            container.Dispose();

            Assert.Equal(1, pooledInstance.DisposeCalled);
        }

        [Fact]
        public void DisposableRegistrationsDisposedWhenMaximumRetainedExceeded()
        {
            var builder = new ContainerBuilder();

            // Only retain 1 instance in the pool.
            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(1);

            var container = builder.Build();

            var pooled1 = container.Resolve<Owned<IPooledService>>();

            var pooledInstance1 = pooled1.Value;

            var pooled2 = container.Resolve<Owned<IPooledService>>();

            var pooledInstance2 = pooled2.Value;

            // Dispose puts it back in the pool.
            pooled1.Dispose();

            // Pooled 2 will not go back in the pool because we have a max capacity of 1.
            // So it's disposed immediately.
            pooled2.Dispose();

            Assert.Equal(0, pooledInstance1.DisposeCalled);

            Assert.Equal(1, pooledInstance2.ReturnCalled);
            Assert.Equal(1, pooledInstance2.DisposeCalled);

            // When the container is disposed, the pooled instance is disposed, but nothing
            // happens to the not-pooled instance.
            container.Dispose();

            Assert.Equal(1, pooledInstance1.DisposeCalled);
            Assert.Equal(1, pooledInstance2.DisposeCalled);
        }
    }
}
