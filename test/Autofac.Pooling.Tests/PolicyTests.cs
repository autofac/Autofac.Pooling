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
                (ctxt, param) => events.Add("before"),
                (ctxt, param, instance) =>
                {
                    events.Add("after");
                    Assert.NotNull(instance);
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

            Assert.Equal(new[] { "before", "after", "return" }, events);
        }

        private class CustomPolicy<TLimit> : IPooledRegistrationPolicy<TLimit>
            where TLimit : class
        {
            private readonly Action<IComponentContext, IEnumerable<Parameter>> _beforeGet;
            private readonly Action<IComponentContext, IEnumerable<Parameter>, TLimit> _afterGet;
            private readonly Func<TLimit, bool> _beforeReturn;

            public int MaximumRetained => 6;

            public CustomPolicy(
                Action<IComponentContext, IEnumerable<Parameter>> beforeGet,
                Action<IComponentContext, IEnumerable<Parameter>, TLimit> afterGet,
                Func<TLimit, bool> beforeReturn)
            {
                _beforeGet = beforeGet;
                _afterGet = afterGet;
                _beforeReturn = beforeReturn;
            }


            public void BeforeGetFromPool(IComponentContext ctxt, IEnumerable<Parameter> parameters)
            {
                _beforeGet(ctxt, parameters);
            }

            public void AfterGetFromPool(IComponentContext ctxt, IEnumerable<Parameter> parameters, TLimit pooledObject)
            {
                _afterGet(ctxt, parameters, pooledObject);
            }

            public bool BeforeReturn(TLimit pooledObject)
            {
                return _beforeReturn(pooledObject);
            }
        }
    }
}
