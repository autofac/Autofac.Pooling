# Autofac.Pooling

Support for pooled instance lifetime scopes in [Autofac](https://autofac.org) dependency injection. Autofac can help you implement a pool of components in your application without you having to write your own pooling implementation, and making these pooled components feel more natural in the world of DI.

[![Build status](https://github.com/autofac/Autofac.Pooling/actions/workflows/main.yml/badge.svg)](https://github.com/autofac/Autofac.Pooling/actions/workflows/main.yml) [![codecov](https://codecov.io/gh/Autofac/Autofac.Pooling/branch/develop/graph/badge.svg)](https://codecov.io/gh/Autofac/Autofac.Pooling) [![NuGet](https://img.shields.io/nuget/v/Autofac.Pooling.svg)](https://nuget.org/packages/Autofac.Pooling)

Please file issues and pull requests for this package in this repository rather than in the Autofac core repo.

- [Documentation](https://autofac.readthedocs.io/advanced/pooled-instances.html)
- [NuGet](https://www.nuget.org/packages/Autofac.Pooling)
- [Contributing](https://autofac.readthedocs.io/en/latest/contributors.html)
- [Open in Visual Studio Code](https://open.vscode.dev/autofac/Autofac.Pooling)

## Quick Start

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

// Register the backing cache in Autofac. Because it is registered here, the
// container owns it and disposes it at shutdown, and it can be shared with the
// rest of the application.
builder.RegisterInstance(new MemoryCache(new MemoryCacheOptions()))
        .As<IMemoryCache>();

// Register the provider itself so Autofac constructs it and injects the cache.
builder.RegisterType<CacheObjectPoolProvider>()
        .SingleInstance();

builder.RegisterType<MyCustomConnection>()
        .As<ICustomConnection>()
        // The provider factory receives the IComponentContext, so it can resolve
        // the provider - and its dependencies - straight from the container.
        .PooledInstancePerLifetimeScope(
            ctx => ctx.Resolve<CacheObjectPoolProvider>());

var container = builder.Build();
```

Autofac still owns *construction* of the pooled instances (they are resolved through the container, so dependency injection and the `IPooledComponent` / `IPooledRegistrationPolicy` callbacks all work as normal). The provider only controls storage and eviction. The provider's `Create<T>(IPooledObjectPolicy<T>)` method is the seam: Autofac hands in its own policy whose `Create()` resolves a fully-injected instance, so your pool delegates construction to it rather than new-ing up objects itself.

A cache-backed provider that takes its cache from Autofac looks like this:

```csharp
public sealed class CacheObjectPoolProvider : ObjectPoolProvider
{
    private readonly IMemoryCache _cache;

    // The cache is injected from Autofac rather than created here, so its
    // lifetime is managed by the container and shared across every pool this
    // provider creates.
    public CacheObjectPoolProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
        => new CacheObjectPool<T>(_cache, policy);
}

public sealed class CacheObjectPool<T> : ObjectPool<T>
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
            // Dispose the instance when the cache eventually evicts it; the pool
            // owns disposal of instances the cache drops.
            var options = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) };
            options.RegisterPostEvictionCallback((_, value, _, _) => (value as IDisposable)?.Dispose());
            _cache.Set(typeof(T), obj, options);
        }
        else if (obj is IDisposable disposable)
        {
            // The pool owns disposal of instances it declines.
            disposable.Dispose();
        }
    }
}
```

The pool does not implement `IDisposable` because it does not own the cache — Autofac creates the `IMemoryCache`, so Autofac disposes it. The pool only takes responsibility for the pooled instances themselves, disposing them when the policy declines a return and when the cache evicts them.

Things to know about custom providers:

- The provider factory is invoked **once** per registration, when the pool is built, resolved from the pool-owning (root) scope. Because the factory gets an `IComponentContext`, the provider can be registered like any other component and resolved from the container, letting Autofac inject its dependencies (the `IMemoryCache` above). Registering the provider and cache as singletons shares one provider and cache across every type that uses them.
- With a custom provider, `IPooledRegistrationPolicy.MaximumRetained` does **not** size the pool — the provider owns sizing and eviction. You can still supply a custom policy alongside the provider with the `PooledInstancePerLifetimeScope(policyFactory, providerFactory)` overload to control the `Get` / `Return` behavior.
- The pool is **shared across all lifetime scopes and threads**, so your pool must be thread-safe.
- **Disposal contract:**
  - If the pool implements `IDisposable`, the container disposes it at container shutdown.
  - Because `ObjectPool<T>.Return(T)` returns `void` (no kept/dropped signal) and there is no "permanently evicted" callback, **the custom pool is responsible for disposing instances it declines on `Return` or evicts asynchronously.** Autofac cannot see those instances.
  - Instances that never entered the pool (because the policy chose not to call the pool) are disposed by normal lifetime scope disposal.

## Get Help

**Need help with Autofac?** We have [a documentation site](https://autofac.readthedocs.io/) as well as [API documentation](https://autofac.org/apidoc/). We're ready to answer your questions on [Stack Overflow](https://stackoverflow.com/questions/tagged/autofac) or check out the [discussion forum](https://groups.google.com/forum/#forum/autofac).
