using Autofac.Core;
using Autofac.Pooling.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Autofac.Pooling.Test
{
    public class ConcurrencyTests
    {
        [Fact]
        public async Task CanUsePoolConcurrently()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<PooledComponent>().As<IPooledService>()
                                                   .PooledInstancePerLifetimeScope();

            var container = builder.Build();

            await Task.WhenAll(Enumerable.Range(0, 2000).Select(i => Task.Run(() =>
            {
                using var scope = container.BeginLifetimeScope();

                scope.Resolve<IPooledService>();
            })));

            container.Dispose();
        }

        [Fact]
        public async Task CanUsePoolConcurrentlyWithCustomPolicyToBlockOnMaxUsage()
        {
            var builder = new ContainerBuilder();

            var blockingPolicy = new BlockingPolicy<PooledComponent>(4);

            builder.RegisterType<PooledComponent>().As<IPooledService>()
                                                   .PooledInstancePerLifetimeScope(blockingPolicy);

            var container = builder.Build();

            await Task.WhenAll(Enumerable.Range(0, 10000).Select(i => Task.Run(() =>
            {
                using var scope = container.BeginLifetimeScope();

                scope.Resolve<IPooledService>();

                Assert.InRange(blockingPolicy.InUseCount, 1, 4);
            })));

            container.Dispose();
        }

        private class BlockingPolicy<TLimit> : DefaultPooledRegistrationPolicy<TLimit>
            where TLimit : class
        {
            private readonly SemaphoreSlim _semaphore;

            public BlockingPolicy(int maxConcurrentInstances) : base(maxConcurrentInstances)
            {
                _semaphore = new SemaphoreSlim(maxConcurrentInstances);
            }

            public int InUseCount => MaximumRetained - _semaphore.CurrentCount;

            public override TLimit Get(IComponentContext context, IEnumerable<Parameter> parameters, Func<TLimit> getFromPool)
            {
                _semaphore.Wait();

                Assert.InRange(_semaphore.CurrentCount, 0, MaximumRetained);

                return base.Get(context, parameters, getFromPool);
            }

            public override bool Return(TLimit pooledObject)
            {
                _semaphore.Release();

                return base.Return(pooledObject);
            }
        }
    }
}
