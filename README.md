# Autofac.Pooling

[![Build status](https://github.com/autofac/Autofac.Pooling/actions/workflows/main.yml/badge.svg?branch=develop)](https://github.com/autofac/Autofac.Pooling/actions/workflows/main.yml)

Support for pooled instance lifetime scopes in Autofac dependency injection.

Autofac can help you implement a pool of components in your application without you having to write your own pooling implementation, and making these pooled components feel more natural in the world of DI.

Please file issues and pull requests for this package in this repository rather than in the Autofac core repo.

- [Documentation](https://autofac.readthedocs.io/advanced/pooled-instances.html)
- [NuGet](https://www.nuget.org/packages/Autofac.Pooling)
- [Contributing](https://autofac.readthedocs.io/en/latest/contributors.html)

## Getting Started

Once you've added a reference to the `Autofac.Pooling` package, you can start using the new `PooledInstancePerLifetimeScope` and `PooledInstancePerMatchingLifetimeScope` methods:

```csharp
var builder = new ContainerBuilder();

builder.RegisterType<MyCustomConnection>()
        .As<ICustomConnection>()
        .PooledInstancePerLifetimeScope();

var container = builder.Build();

using (var scope = container.BeginLifetimeScope())
{
    // Creates a new instance of MyCustomConnection
    var instance = scope.Resolve<ICustomConnection>();

    instance.DoSomething();
}

// When the scope ends, the instance of MyCustomConnection
// is returned to the pool, rather than being disposed.

using (var scope2 = container.BeginLifetimeScope())
{
    // Does **not** create a new instance, but instead gets the
    // previous instance from the pool.
    var instance = scope.Resolve<ICustomConnection>();

    instance.DoSomething();
}

// Instance gets returned back to the pool again at the
// end of the lifetime scope.
```

## Custom Pool Providers

By default, pooled instances are stored in a `DefaultObjectPool` sized from the policy's `MaximumRetained` value. If you need full control over *where* instances are stored and *when* they are evicted (for example, a cache-backed pool with an idle timeout), you can supply your own `Microsoft.Extensions.ObjectPool.ObjectPoolProvider`:

```csharp
var builder = new ContainerBuilder();

builder.RegisterType<MyCustomConnection>()
        .As<ICustomConnection>()
        .PooledInstancePerLifetimeScope(ctx => new CacheObjectPoolProvider());

var container = builder.Build();
```

Autofac still owns *construction* of the pooled instances (they are resolved through the container, so dependency injection and the `IPooledComponent` / `IPooledRegistrationPolicy` callbacks all work as normal). The provider only controls storage and eviction. The provider's `Create<T>(IPooledObjectPolicy<T>)` method is the seam: Autofac hands in its own policy whose `Create()` resolves a fully-injected instance, so your pool delegates construction to it rather than new-ing up objects itself.

A minimal cache-backed provider looks like this:

```csharp
public sealed class CacheObjectPoolProvider : ObjectPoolProvider
{
    // One shared cache backs every pool this provider creates.
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
        => new CacheObjectPool<T>(_cache, policy);
}

public sealed class CacheObjectPool<T> : ObjectPool<T>, IDisposable
    where T : class
{
    private readonly IMemoryCache _cache;
    private readonly IPooledObjectPolicy<T> _policy;

    public CacheObjectPool(IMemoryCache cache, IPooledObjectPolicy<T> policy)
    {
        _cache = cache;
        _policy = policy;
    }

    public override T Get()
        // Ask the cache for a stored instance; build a new one through Autofac on a miss.
        => _cache.TryGetValue(typeof(T), out T? item) && item is not null
            ? item
            : _policy.Create();

    public override void Return(T obj)
    {
        // The policy decides whether the instance is fit to be retained.
        if (_policy.Return(obj))
        {
            _cache.Set(typeof(T), obj, /* eviction options */ TimeSpan.FromMinutes(5));
        }
        else if (obj is IDisposable disposable)
        {
            // The pool owns disposal of instances it declines.
            disposable.Dispose();
        }
    }

    public void Dispose() => _cache.Dispose();
}
```

Things to know about custom providers:

- The provider factory is invoked **once** per registration, when the pool is built, resolved from the pool-owning (root) scope. You can resolve the provider as a singleton component and share it across several registrations (the cache-backed example above shares one cache for every type it pools).
- With a custom provider, `IPooledRegistrationPolicy.MaximumRetained` does **not** size the pool — the provider owns sizing and eviction. You can still supply a custom policy alongside the provider with the `PooledInstancePerLifetimeScope(policyFactory, providerFactory)` overload to control the `Get` / `Return` behavior.
- The pool is **shared across all lifetime scopes and threads**, so your pool must be thread-safe.
- **Disposal contract:**
  - If the pool implements `IDisposable`, the container disposes it at container shutdown.
  - Because `ObjectPool<T>.Return(T)` returns `void` (no kept/dropped signal) and there is no "permanently evicted" callback, **the custom pool is responsible for disposing instances it declines on `Return` or evicts asynchronously.** Autofac cannot see those instances.
  - Instances that never entered the pool (because the policy chose not to call the pool) are disposed by normal lifetime scope disposal.

## Get Help

**Need help with Autofac?** We have [a documentation site](https://autofac.readthedocs.io/) as well as [API documentation](https://autofac.org/apidoc/). We're ready to answer your questions on [Stack Overflow](https://stackoverflow.com/questions/tagged/autofac) or check out the [discussion forum](https://groups.google.com/forum/#forum/autofac).
