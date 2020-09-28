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

namespace Autofac.Pooling
{
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

            var callback = regData.DeferredCallback;

            if (callback is null)
            {
                throw new NotSupportedException(RegistrationExtensionsResources.RequiresCallbackContainer);
            }

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
    }
}