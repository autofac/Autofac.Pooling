// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Activators.ProvidedInstance;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Middleware;
using Autofac.Core.Resolving.Pipeline;
using Microsoft.Extensions.ObjectPool;

namespace Autofac.Pooling;

/// <summary>
/// Provides registration methods for setting up pooled instances.
/// </summary>
public static class RegistrationExtensions
{
    /// <summary>
    /// Configure the component so that every dependent component or manual resolve within a single <see cref="ILifetimeScope"/>
    /// will return the same, shared instance, retrieved from a single pool of instances shared by all lifetime scopes.
    /// When the scope ends, the instance will be returned to the pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size of the pool created with this method defaults to twice the number of processors (<see cref="Environment.ProcessorCount"/> x 2).
    /// If more instances are requested than the pool size, those instances may not be returned to the pool, but will instead be disposed/discarded.
    /// </para>
    ///
    /// <para>
    /// If a component needs to perform behaviour when it is retrieved from or returned to the pool, it can implement <see cref="IPooledComponent"/>,
    /// or use the overload of this method that accepts a custom <see cref="IPooledRegistrationPolicy{TLimit}"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">The registration.</param>
    /// <returns>The registration builder.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        RegisterPooled(registration, new DefaultPooledRegistrationPolicy<TLimit>(), null);

        return registration;
    }

    /// <summary>
    /// Configure the component so that every dependent component or manual resolve within a single <see cref="ILifetimeScope"/>
    /// will return the same, shared instance, retrieved from a single pool of instances shared by all lifetime scopes.
    /// When the scope ends, the instance will be returned to the pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size of the pool created with this method is equal to <paramref name="maximumRetainedInstances"/>.
    /// If more instances are requested than the pool size, those instances may not be returned to the pool, but will instead be disposed/discarded.
    /// </para>
    ///
    /// <para>
    /// If a component needs to perform behaviour when it is retrieved from or returned to the pool, it can implement <see cref="IPooledComponent"/>,
    /// or use the overload of this method that accepts a custom <see cref="IPooledRegistrationPolicy{TLimit}"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">The registration.</param>
    /// <param name="maximumRetainedInstances">The maximum number of instances to retain in the pool.</param>
    /// <returns>The registration builder.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               int maximumRetainedInstances)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        RegisterPooled(registration, new DefaultPooledRegistrationPolicy<TLimit>(maximumRetainedInstances), null);

        return registration;
    }

    /// <summary>
    /// Configure the component so that every dependent component or manual resolve within a single <see cref="ILifetimeScope"/>
    /// will return the same, shared instance, retrieved from a single pool of instances shared by all lifetime scopes.
    /// When the scope ends, the instance will be returned to the pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method accepts a custom <see cref="IPooledRegistrationPolicy{TLimit}"/> that provides fine-grained control of the retrieval
    /// of instances from the pool, and allows the implementer to choose whether or not the instance should even be returned to the pool.
    /// </para>
    ///
    /// <para>
    /// The size of the pool created with this method is equal to the <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/> value on the
    /// <paramref name="poolPolicy"/>.
    /// If more instances are requested than the pool size, those instances may not be returned to the pool, but will instead be disposed/discarded.
    /// </para>
    /// </remarks>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">The registration.</param>
    /// <param name="poolPolicy">A custom policy for controlling pool behaviour.</param>
    /// <returns>The registration builder.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               IPooledRegistrationPolicy<TLimit> poolPolicy)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        RegisterPooled(registration, poolPolicy, null);

        return registration;
    }

    /// <summary>
    /// Configure the component with a custom pool factory. Must be followed by <see cref="PooledPerLifetimeScope{TLimit, TActivatorData, TSingleRegistrationStyle}"/>.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">The registration.</param>
    /// <param name="poolFactory">A factory that returns the <see cref="ObjectPool{TLimit}"/> to use.</param>
    /// <returns>The registration builder for chaining.</returns>
    /// <summary>
    /// Configure the component so that every dependent component or manual resolve within a single <see cref="ILifetimeScope"/>
    /// will return the same, shared instance, retrieved from a pool of instances shared by all lifetime scopes.
    /// When the scope ends, the instance will be returned to the pool.
    /// Returns an <see cref="IPooledRegistrationBuilder{TLimit, TActivatorData, TSingleRegistrationStyle}"/>
    /// for further configuration via <see cref="ConfigurePool{TLimit, TActivatorData, TSingleRegistrationStyle}"/>
    /// and <see cref="ConfigureStrategy{TLimit, TActivatorData, TSingleRegistrationStyle}"/>.
    /// When no additional configuration is provided, defaults apply.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">The registration.</param>
    /// <returns>A pooled registration builder for further configuration.</returns>
    public static IPooledRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledPerLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null) throw new ArgumentNullException(nameof(registration));

        // Lazy routing: defer to registry build time, when Metadata is populated.
        var regData = registration.RegistrationData;
        regData.Lifetime = new PooledLifetime();
        regData.Sharing = InstanceSharing.None;

        var callback = regData.DeferredCallback
            ?? throw new NotSupportedException(RegistrationExtensionsResources.RequiresCallbackContainer);

        if (registration.ActivatorData.Activator is ProvidedInstanceActivator)
            throw new NotSupportedException(RegistrationExtensionsResources.CannotUseProvidedInstances);

        var original = callback.Callback;
        callback.Callback = registry =>
        {
            if (!(regData.Lifetime is PooledLifetime)) { original(registry); return; }

            // Begin: inlined from RegisterPooled(IPooledRegistrationPolicy)
            if (registration.ResolvePipeline.Middleware.Any(c => c is CoreEventMiddleware ev && ev.EventType == ResolveEventType.OnRelease))
                throw new NotSupportedException(RegistrationExtensionsResources.OnReleaseNotSupported);

            var pooledInstanceService = new UniqueService();
            var instanceActivator = registration.ActivatorData.Activator;

            var pooledInstanceRegistration = new ComponentRegistration(
                Guid.NewGuid(), instanceActivator, RootScopeLifetime.Instance,
                InstanceSharing.None, InstanceOwnership.ExternallyOwned,
                registration.ResolvePipeline,
                new[] { pooledInstanceService },
                new Dictionary<string, object?>());
            registry.Register(pooledInstanceRegistration);

            var poolService = new PoolService(pooledInstanceRegistration);

            var pf = regData.Metadata.TryGetValue("PoolFactory", out var pv)
                ? pv as Func<IComponentContext, ObjectPool<TLimit>>
                : null;
            var sf = regData.Metadata.TryGetValue("StrategyFactory", out var sv)
                ? sv as Func<IComponentContext, IPooledRegistrationPolicy<TLimit>>
                : null;

            if (pf is not null && sf is not null)
            {
                registry.Register(new ComponentRegistration(Guid.NewGuid(), new PoolActivator<TLimit>(pf), RootScopeLifetime.Instance, InstanceSharing.Shared, InstanceOwnership.OwnedByLifetimeScope, new[] { poolService }, new Dictionary<string, object?>()));
                registry.Register(new ComponentRegistration(Guid.NewGuid(), new PoolGetActivator<TLimit>(poolService, sf), CurrentScopeLifetime.Instance, InstanceSharing.Shared, InstanceOwnership.OwnedByLifetimeScope, regData.Services, regData.Metadata));
            }
            else if (sf is not null)
            {
                registry.Register(new ComponentRegistration(Guid.NewGuid(), new PoolActivator<TLimit>(pooledInstanceService, sf), RootScopeLifetime.Instance, InstanceSharing.Shared, InstanceOwnership.OwnedByLifetimeScope, new[] { poolService }, new Dictionary<string, object?>()));
                registry.Register(new ComponentRegistration(Guid.NewGuid(), new PoolGetActivator<TLimit>(poolService, sf), CurrentScopeLifetime.Instance, InstanceSharing.Shared, InstanceOwnership.OwnedByLifetimeScope, regData.Services, regData.Metadata));
            }
            else if (pf is not null)
            {
                registry.Register(new ComponentRegistration(Guid.NewGuid(), new PoolActivator<TLimit>(pf), RootScopeLifetime.Instance, InstanceSharing.Shared, InstanceOwnership.OwnedByLifetimeScope, new[] { poolService }, new Dictionary<string, object?>()));
                registry.Register(new ComponentRegistration(Guid.NewGuid(), new PoolGetActivator<TLimit>(poolService, static _ => new DefaultPooledRegistrationPolicy<TLimit>()), CurrentScopeLifetime.Instance, InstanceSharing.Shared, InstanceOwnership.OwnedByLifetimeScope, regData.Services, regData.Metadata));
            }
            else
            {
                registry.Register(new ComponentRegistration(Guid.NewGuid(), new PoolActivator<TLimit>(pooledInstanceService, new DefaultPooledRegistrationPolicy<TLimit>()), RootScopeLifetime.Instance, InstanceSharing.Shared, InstanceOwnership.OwnedByLifetimeScope, new[] { poolService }, new Dictionary<string, object?>()));
                registry.Register(new ComponentRegistration(Guid.NewGuid(), new PoolGetActivator<TLimit>(poolService, new DefaultPooledRegistrationPolicy<TLimit>()), CurrentScopeLifetime.Instance, InstanceSharing.Shared, InstanceOwnership.OwnedByLifetimeScope, regData.Services, regData.Metadata));
            }

            foreach (var srv in regData.Services)
                registry.RegisterServiceMiddleware(srv, new PooledInstanceUnpackMiddleware<TLimit>(), MiddlewareInsertionMode.StartOfPhase);
        };

        return new PooledRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>(registration);
    }

    /// <summary>
    /// Sets a custom pool factory on a pooled registration. Must follow <see cref="PooledPerLifetimeScope{TLimit, TActivatorData, TSingleRegistrationStyle}"/>.
    /// When set, <see cref="ObjectPool{TLimit}.Get"/> and <see cref="ObjectPool{TLimit}.Return"/> are controlled by the provided pool.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="builder">The pooled registration builder.</param>
    /// <param name="poolFactory">A factory that returns the <see cref="ObjectPool{TLimit}"/> to use.</param>
    /// <returns>The pooled registration builder for chaining.</returns>
    public static IPooledRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           ConfigurePool<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IPooledRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> builder,
               Func<IComponentContext, ObjectPool<TLimit>> poolFactory)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (poolFactory == null) throw new ArgumentNullException(nameof(poolFactory));
        builder.RegistrationData.Metadata["PoolFactory"] = poolFactory;
        return builder;
    }

    /// <summary>
    /// Sets a custom strategy factory on a pooled registration. Must follow <see cref="PooledPerLifetimeScope{TLimit, TActivatorData, TSingleRegistrationStyle}"/>.
    /// When set, the strategy's <see cref="IPooledRegistrationPolicy{TLimit}.Get"/> receives full context at resolve time.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="builder">The pooled registration builder.</param>
    /// <param name="strategyFactory">A factory that returns the <see cref="IPooledRegistrationPolicy{TLimit}"/> to use.</param>
    /// <returns>The pooled registration builder for chaining.</returns>
    public static IPooledRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           ConfigureStrategy<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IPooledRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> builder,
               Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> strategyFactory)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (strategyFactory == null) throw new ArgumentNullException(nameof(strategyFactory));
        builder.RegistrationData.Metadata["StrategyFactory"] = strategyFactory;
        return builder;
    }

    /// <summary>
    /// Configure the component so that every dependent component or manual resolve within
    /// a <see cref="ILifetimeScope"/> tagged with any of the provided tags value gets the same, shared instance,
    /// retrieved from a single pool of instances shared by all lifetime scopes.
    /// When the scope ends, the instance will be returned to the pool.
    /// Dependent components in lifetime scopes that are children of the tagged scope will
    /// share the parent's instance. If no appropriately tagged scope can be found in the
    /// hierarchy an <see cref="DependencyResolutionException"/> is thrown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size of the pool created with this method defaults to twice the number of processors (<see cref="Environment.ProcessorCount"/> x 2).
    /// If more instances are requested than the pool size, those instances may not be returned to the pool, but will instead be disposed/discarded.
    /// </para>
    ///
    /// <para>
    /// If a component needs to perform behaviour when it is retrieved from or returned to the pool, it can implement <see cref="IPooledComponent"/>,
    /// or use the overload of this method that accepts a custom <see cref="IPooledRegistrationPolicy{TLimit}"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">The registration.</param>
    /// <param name="lifetimeScopeTags">Tags applied to matching lifetime scopes.</param>
    /// <returns>The registration builder.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerMatchingLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               params object[] lifetimeScopeTags)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        RegisterPooled(registration, new DefaultPooledRegistrationPolicy<TLimit>(), lifetimeScopeTags);

        return registration;
    }

    /// <summary>
    /// Configure the component so that every dependent component or manual resolve within
    /// a <see cref="ILifetimeScope"/> tagged with any of the provided tags value gets the same, shared instance,
    /// retrieved from a single pool of instances shared by all lifetime scopes.
    /// When the scope ends, the instance will be returned to the pool.
    /// Dependent components in lifetime scopes that are children of the tagged scope will
    /// share the parent's instance. If no appropriately tagged scope can be found in the
    /// hierarchy an <see cref="DependencyResolutionException"/> is thrown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size of the pool created with this method is equal to <paramref name="maximumRetainedInstances"/>.
    /// If more instances are requested than the pool size, those instances may not be returned to the pool, but will instead be disposed/discarded.
    /// </para>
    ///
    /// <para>
    /// If a component needs to perform behaviour when it is retrieved from or returned to the pool, it can implement <see cref="IPooledComponent"/>,
    /// or use the overload of this method that accepts a custom <see cref="IPooledRegistrationPolicy{TLimit}"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">The registration.</param>
    /// <param name="maximumRetainedInstances">The maximum number of instances to retain in the pool.</param>
    /// <param name="lifetimeScopeTags">Tags applied to matching lifetime scopes.</param>
    /// <returns>The registration builder.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerMatchingLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               int maximumRetainedInstances,
               params object[] lifetimeScopeTags)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        RegisterPooled(registration, new DefaultPooledRegistrationPolicy<TLimit>(maximumRetainedInstances), lifetimeScopeTags);

        return registration;
    }

    /// <summary>
    /// Configure the component so that every dependent component or manual resolve within
    /// a <see cref="ILifetimeScope"/> tagged with any of the provided tags value gets the same, shared instance,
    /// retrieved from a single pool of instances shared by all lifetime scopes.
    /// When the scope ends, the instance will be returned to the pool.
    /// Dependent components in lifetime scopes that are children of the tagged scope will
    /// share the parent's instance. If no appropriately tagged scope can be found in the
    /// hierarchy an <see cref="DependencyResolutionException"/> is thrown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method accepts a custom <see cref="IPooledRegistrationPolicy{TLimit}"/> that provides fine-grained control of the retrieval
    /// of instances from the pool, and allows the implementer to choose whether or not the instance should even be returned to the pool.
    /// </para>
    ///
    /// <para>
    /// The size of the pool created with this method is equal to the <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/> value on the
    /// <paramref name="poolPolicy"/>.
    /// If more instances are requested than the pool size, those instances may not be returned to the pool, but will instead be disposed/discarded.
    /// </para>
    /// </remarks>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">The registration.</param>
    /// <param name="poolPolicy">A custom policy for controlling pool behaviour.</param>
    /// <param name="lifetimeScopeTags">Tags applied to matching lifetime scopes.</param>
    /// <returns>The registration builder.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerMatchingLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               IPooledRegistrationPolicy<TLimit> poolPolicy,
               params object[] lifetimeScopeTags)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        RegisterPooled(registration, poolPolicy, lifetimeScopeTags);

        return registration;
    }

    private static void RegisterPooled<TLimit, TActivatorData, TSingleRegistrationStyle>(
        IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
        IPooledRegistrationPolicy<TLimit> registrationPolicy,
        object[]? tags)
        where TSingleRegistrationStyle : SingleRegistrationStyle
        where TActivatorData : IConcreteActivatorData
        where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        // Mark the lifetime appropriately.
        var regData = registration.RegistrationData;

        regData.Lifetime = new PooledLifetime();
        regData.Sharing = InstanceSharing.None;

        var callback = regData.DeferredCallback ?? throw new NotSupportedException(RegistrationExtensionsResources.RequiresCallbackContainer);

        if (registration.ActivatorData.Activator is ProvidedInstanceActivator)
        {
            // Can't use provided instance activators with pooling (because it would try to repeatedly activate).
            throw new NotSupportedException(RegistrationExtensionsResources.CannotUseProvidedInstances);
        }

        var original = callback.Callback;

        Action<IComponentRegistryBuilder> newCallback = registry =>
        {
            // Only do the additional registrations if we are still using a PooledLifetime.
            if (!(regData.Lifetime is PooledLifetime))
            {
                original(registry);
                return;
            }

            var pooledInstanceService = new UniqueService();

            var instanceActivator = registration.ActivatorData.Activator;

            if (registration.ResolvePipeline.Middleware.Any(c => c is CoreEventMiddleware ev && ev.EventType == ResolveEventType.OnRelease))
            {
                // OnRelease shouldn't be used with pooled instances, because if a policy chooses not to return them to the pool,
                // the Disposal will be fired, not the OnRelease call; this means that OnRelease wouldn't fire until the container is disposed,
                // which is not what we want.
                throw new NotSupportedException(RegistrationExtensionsResources.OnReleaseNotSupported);
            }

            // First, we going to create a pooled instance activator, that will be resolved when we want to
            // **actually** resolve a new instance (during 'Create').
            // The instances themselves are owned by the pool, and will be disposed when the pool disposes
            // (or when the instance is not returned to the pool).
            var pooledInstanceRegistration = new ComponentRegistration(
                Guid.NewGuid(),
                instanceActivator,
                RootScopeLifetime.Instance,
                InstanceSharing.None,
                InstanceOwnership.ExternallyOwned,
                registration.ResolvePipeline,
                new[] { pooledInstanceService },
                new Dictionary<string, object?>());

            registry.Register(pooledInstanceRegistration);

            var poolService = new PoolService(pooledInstanceRegistration);

            var poolRegistration = new ComponentRegistration(
                Guid.NewGuid(),
                new PoolActivator<TLimit>(pooledInstanceService, registrationPolicy),
                RootScopeLifetime.Instance,
                InstanceSharing.Shared,
                InstanceOwnership.OwnedByLifetimeScope,
                new[] { poolService },
                new Dictionary<string, object?>());

            registry.Register(poolRegistration);

            var pooledGetLifetime = tags is null ? CurrentScopeLifetime.Instance : new MatchingScopeLifetime(tags);

            // Next, create a new registration with a custom activator, that copies metadata and services from
            // the original registration. This registration will access the pool and return an instance from it.
            var poolGetRegistration = new ComponentRegistration(
                Guid.NewGuid(),
                new PoolGetActivator<TLimit>(poolService, registrationPolicy),
                pooledGetLifetime,
                InstanceSharing.Shared,
                InstanceOwnership.OwnedByLifetimeScope,
                regData.Services,
                regData.Metadata);

            registry.Register(poolGetRegistration);

            // Finally, add a service pipeline stage to just before the sharing middleware, for each supported service, to extract the pooled instance from the pool instance container.
            foreach (var srv in regData.Services)
            {
                registry.RegisterServiceMiddleware(srv, new PooledInstanceUnpackMiddleware<TLimit>(), MiddlewareInsertionMode.StartOfPhase);
            }
        };

        callback.Callback = newCallback;
    }

    private static void RegisterPooled<TLimit, TActivatorData, TSingleRegistrationStyle>(
        IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
        Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> policyFactory,
        object[]? tags)
        where TSingleRegistrationStyle : SingleRegistrationStyle
        where TActivatorData : IConcreteActivatorData
        where TLimit : class
    {
        if (registration == null) throw new ArgumentNullException(nameof(registration));
        if (policyFactory == null) throw new ArgumentNullException(nameof(policyFactory));

        var regData = registration.RegistrationData;
        regData.Lifetime = new PooledLifetime();
        regData.Sharing = InstanceSharing.None;

        var callback = regData.DeferredCallback ?? throw new NotSupportedException(RegistrationExtensionsResources.RequiresCallbackContainer);
        if (registration.ActivatorData.Activator is ProvidedInstanceActivator) throw new NotSupportedException(RegistrationExtensionsResources.CannotUseProvidedInstances);

        var original = callback.Callback;
        callback.Callback = registry =>
        {
            if (!(regData.Lifetime is PooledLifetime)) { original(registry); return; }

            var pooledInstanceService = new UniqueService();
            var instanceActivator = registration.ActivatorData.Activator;

            if (registration.ResolvePipeline.Middleware.Any(c => c is CoreEventMiddleware ev && ev.EventType == ResolveEventType.OnRelease))
            {
                throw new NotSupportedException(RegistrationExtensionsResources.OnReleaseNotSupported);
            }

            var pooledInstanceRegistration = new ComponentRegistration(
                Guid.NewGuid(), instanceActivator, RootScopeLifetime.Instance,
                InstanceSharing.None, InstanceOwnership.ExternallyOwned,
                registration.ResolvePipeline,
                new[] { pooledInstanceService },
                new Dictionary<string, object?>());
            registry.Register(pooledInstanceRegistration);

            var poolService = new PoolService(pooledInstanceRegistration);

            registry.Register(new ComponentRegistration(
                Guid.NewGuid(),
                new PoolActivator<TLimit>(pooledInstanceService, policyFactory),
                RootScopeLifetime.Instance, InstanceSharing.Shared,
                InstanceOwnership.OwnedByLifetimeScope,
                new[] { poolService },
                new Dictionary<string, object?>()));

            var pooledGetLifetime = tags is null
                ? CurrentScopeLifetime.Instance
                : new MatchingScopeLifetime(tags);

            registry.Register(new ComponentRegistration(
                Guid.NewGuid(),
                new PoolGetActivator<TLimit>(poolService, policyFactory),
                pooledGetLifetime, InstanceSharing.Shared,
                InstanceOwnership.OwnedByLifetimeScope,
                regData.Services, regData.Metadata));

            foreach (var srv in regData.Services)
            {
                registry.RegisterServiceMiddleware(srv, new PooledInstanceUnpackMiddleware<TLimit>(), MiddlewareInsertionMode.StartOfPhase);
            }
        };
    }

    private static void RegisterPooled<TLimit, TActivatorData, TSingleRegistrationStyle>(
        IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
        Func<IComponentContext, ObjectPool<TLimit>> poolFactory,
        Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> strategyFactory,
        object[]? tags)
        where TSingleRegistrationStyle : SingleRegistrationStyle
        where TActivatorData : IConcreteActivatorData
        where TLimit : class
    {
        if (registration == null) throw new ArgumentNullException(nameof(registration));
        if (poolFactory == null) throw new ArgumentNullException(nameof(poolFactory));
        if (strategyFactory == null) throw new ArgumentNullException(nameof(strategyFactory));

        var regData = registration.RegistrationData;
        regData.Lifetime = new PooledLifetime();
        regData.Sharing = InstanceSharing.None;

        var callback = regData.DeferredCallback ?? throw new NotSupportedException(RegistrationExtensionsResources.RequiresCallbackContainer);
        if (registration.ActivatorData.Activator is ProvidedInstanceActivator) throw new NotSupportedException(RegistrationExtensionsResources.CannotUseProvidedInstances);

        var original = callback.Callback;
        callback.Callback = registry =>
        {
            if (!(regData.Lifetime is PooledLifetime)) { original(registry); return; }

            var pooledInstanceService = new UniqueService();
            var instanceActivator = registration.ActivatorData.Activator;

            if (registration.ResolvePipeline.Middleware.Any(c => c is CoreEventMiddleware ev && ev.EventType == ResolveEventType.OnRelease))
            {
                throw new NotSupportedException(RegistrationExtensionsResources.OnReleaseNotSupported);
            }

            var pooledInstanceRegistration = new ComponentRegistration(
                Guid.NewGuid(), instanceActivator, RootScopeLifetime.Instance,
                InstanceSharing.None, InstanceOwnership.ExternallyOwned,
                registration.ResolvePipeline,
                new[] { pooledInstanceService },
                new Dictionary<string, object?>());
            registry.Register(pooledInstanceRegistration);

            var poolService = new PoolService(pooledInstanceRegistration);

            registry.Register(new ComponentRegistration(
                Guid.NewGuid(),
                new PoolActivator<TLimit>(poolFactory),
                RootScopeLifetime.Instance, InstanceSharing.Shared,
                InstanceOwnership.OwnedByLifetimeScope,
                new[] { poolService },
                new Dictionary<string, object?>()));

            var pooledGetLifetime = tags is null
                ? CurrentScopeLifetime.Instance
                : new MatchingScopeLifetime(tags);

            registry.Register(new ComponentRegistration(
                Guid.NewGuid(),
                new PoolGetActivator<TLimit>(poolService, strategyFactory),
                pooledGetLifetime, InstanceSharing.Shared,
                InstanceOwnership.OwnedByLifetimeScope,
                regData.Services, regData.Metadata));

            foreach (var srv in regData.Services)
            {
                registry.RegisterServiceMiddleware(srv, new PooledInstanceUnpackMiddleware<TLimit>(), MiddlewareInsertionMode.StartOfPhase);
            }
        };
    }
}
