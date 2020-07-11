using Autofac.Core;
using Autofac.Pooling.Tests.Types;
using System;
using System.Collections.Generic;
using Xunit;

namespace Autofac.Pooling.Tests
{
    public class PolicyTests
    {
        [Fact]
        public void PolicyIsNotifiedAtCorrectPoints()
        {
            var builder = new ContainerBuilder();

            var events = new List<string>();

            var policy = new CustomPolicy<PooledComponent>(
                (ctxt, param, get) => {
                    events.Add("get");
                    return get();
                },
                (instance) =>
                {
                    events.Add("return");
                    return true;
                });

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(policy);

            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                scope.Resolve<IPooledService>();
            }

            Assert.Equal(new[] { "get", "return" }, events);
        }

        [Fact]
        public void PolicyCanChooseNotToGetFromThePool()
        {
            var builder = new ContainerBuilder();

            var returnCalled = false;

            var policy = new CustomPolicy<PooledComponent>(
                (ctxt, param, get) => {
                    return new PooledComponent();
                },
                (instance) =>
                {
                    returnCalled = true;
                    return true;
                });

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(policy);

            var container = builder.Build();

            IPooledService pooledInstance;

            using (var scope = container.BeginLifetimeScope())
            {
                pooledInstance = scope.Resolve<IPooledService>();

                Assert.Equal(0, pooledInstance.GetCalled);
            }

            Assert.Equal(0, pooledInstance.ReturnCalled);
            Assert.Equal(1, pooledInstance.DisposeCalled);
            Assert.False(returnCalled);

            container.Dispose();
        }

        [Fact]
        public void PolicyCanChooseNotToReturnToThePool()
        {
            var builder = new ContainerBuilder();

            var counter = 0;

            var policy = new CustomPolicy<PooledComponent>(
                (ctxt, param, get) => {
                    counter++;
                    return get();
                },
                (instance) =>
                {
                    counter--;
                    return false;
                });

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(policy);

            var container = builder.Build();

            IPooledService pooledInstance;

            using (var scope = container.BeginLifetimeScope())
            {
                pooledInstance = scope.Resolve<IPooledService>();

                Assert.Equal(1, counter);
            }

            Assert.Equal(0, counter);
            Assert.Equal(1, pooledInstance.GetCalled);
            Assert.Equal(1, pooledInstance.ReturnCalled);
            Assert.Equal(1, pooledInstance.DisposeCalled);

            container.Dispose();

            Assert.Equal(1, pooledInstance.DisposeCalled);
        }

        private class CustomPolicy<TLimit> : IPooledRegistrationPolicy<TLimit>
            where TLimit : class
        {
            private readonly Func<IComponentContext, IEnumerable<Parameter>, Func<TLimit>, TLimit> _get;
            private readonly Func<TLimit, bool> _return;

            public int MaximumRetained => 6;

            public CustomPolicy(
                Func<IComponentContext, IEnumerable<Parameter>, Func<TLimit>, TLimit> get,
                Func<TLimit, bool> @return)
            {
                _get = get;
                _return = @return;
            }

            public TLimit Get(IComponentContext context, IEnumerable<Parameter> parameters, Func<TLimit> getFromPool)
            {
                return _get(context, parameters, getFromPool);
            }

            public bool Return(TLimit pooledObject)
            {
                return _return(pooledObject);
            }
        }
    }
}
