using Autofac.Core;
using Autofac.Pooling.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Autofac.Pooling.Test
{
    public class PolicyTests
    {
        [Fact]
        public void PolicyIsNotifiedAtCorrectPoints()
        {
            var builder = new ContainerBuilder();

            var events = new List<string>();

            var policy = new CustomPolicy<PooledComponent>(
                (ctxt, param, getCallback) => {
                    events.Add("get");
                    return getCallback();
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
                (ctxt, param, getCallback) => {
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
                (ctxt, param, getCallback) => {
                    counter++;
                    return getCallback();
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

        [Fact]
        public void PolicyCanSeeParametersFromThePooledServiceResolve()
        {
            var builder = new ContainerBuilder();

            List<Parameter> policyReceivedParameters = new List<Parameter>();

            var policy = new CustomPolicy<PooledComponent>(
                (ctxt, param, getCallback) => {

                    policyReceivedParameters.AddRange(param);

                    return getCallback();
                },
                (instance) =>
                {
                    return false;
                });

            builder.RegisterType<PooledComponent>().As<IPooledService>().PooledInstancePerLifetimeScope(policy);

            var container = builder.Build();

            IPooledService pooledInstance;

            using (var scope = container.BeginLifetimeScope())
            {
                pooledInstance = scope.Resolve<IPooledService>(new NamedParameter("Val1", 123), new TypedParameter(typeof(int), 456));
            }

            Assert.Collection(policyReceivedParameters,
                p =>
                {
                    Assert.Equal("Val1", (p as NamedParameter)?.Name);
                },
                p =>
                {
                    Assert.Equal(456, (p as TypedParameter)?.Value);
                });

            container.Dispose();
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
