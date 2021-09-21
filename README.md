# Autofac.Pooling

[![Build status](https://ci.appveyor.com/api/projects/status/8lvj9casnf84h2u8?svg=true)](https://ci.appveyor.com/project/Autofac/autofac-pooling) [![Open in Visual Studio Code](https://open.vscode.dev/badges/open-in-vscode.svg)](https://open.vscode.dev/autofac/Autofac.Pooling)

Support for pooled instance lifetime scopes in Autofac dependency injection.

Autofac can help you implement a pool of components in your application without you having to write your
own pooling implementation, and making these pooled components feel more natural in the world of DI.

Please file issues and pull requests for this package in this repository rather than in the Autofac core repo.

- [Documentation](https://autofac.readthedocs.io/advanced/pooled-instances.html)
- [NuGet](https://www.nuget.org/packages/Autofac.Pooling)
- [Contributing](https://autofac.readthedocs.io/en/latest/contributors.html)

## Getting Started

Once you've added a reference to the `Autofac.Pooling` package, you can start using
the new `PooledInstancePerLifetimeScope` and `PooledInstancePerMatchingLifetimeScope`
methods:

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

## Get Help

**Need help with Autofac?** We have [a documentation site](https://autofac.readthedocs.io/) as well as [API documentation](https://autofac.org/apidoc/). We're ready to answer your questions on [Stack Overflow](https://stackoverflow.com/questions/tagged/autofac)
or check out the [discussion forum](https://groups.google.com/forum/#forum/autofac).
