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
    /// Configures the component so that every dependent component or manual
    /// resolve within a single <see cref="ILifetimeScope"/> shares one instance
    /// taken from a pool, returning it to the pool when the scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <remarks>
    /// <para>
    /// The pool retains up to twice the processor count
    /// (<see cref="Environment.ProcessorCount"/> x 2) instances. Instances
    /// requested beyond that are disposed or discarded instead of being
    /// retained.
    /// </para>
    /// <para>
    /// To run behavior when an instance is taken from or returned to the pool,
    /// implement <see cref="IPooledComponent"/> on the component, or use an
    /// overload that accepts a custom
    /// <see cref="IPooledRegistrationPolicy{TLimit}"/>.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> is <see langword="null"/>.
    /// </exception>
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
    /// Configures the component so that every dependent component or manual
    /// resolve within a single <see cref="ILifetimeScope"/> shares one instance
    /// taken from a pool, returning it to the pool when the scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="maximumRetainedInstances">
    /// The maximum number of instances to retain in the pool.
    /// </param>
    /// <remarks>
    /// <para>
    /// The pool retains up to <paramref name="maximumRetainedInstances"/>
    /// instances. Instances requested beyond that are disposed or discarded
    /// instead of being retained.
    /// </para>
    /// <para>
    /// To run behavior when an instance is taken from or returned to the pool,
    /// implement <see cref="IPooledComponent"/> on the component, or use an
    /// overload that accepts a custom
    /// <see cref="IPooledRegistrationPolicy{TLimit}"/>.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> is <see langword="null"/>.
    /// </exception>
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
    /// Configures the component so that every dependent component or manual
    /// resolve within a single <see cref="ILifetimeScope"/> shares one instance
    /// taken from a pool governed by a custom policy, returning it to the pool
    /// when the scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="poolPolicy">
    /// A custom policy that controls pool behavior.
    /// </param>
    /// <remarks>
    /// <para>
    /// The policy gives fine-grained control over how instances are retrieved
    /// from the pool, including whether an instance is returned to the pool at
    /// all.
    /// </para>
    /// <para>
    /// The pool retains up to
    /// <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/>
    /// instances. Instances requested beyond that are disposed or discarded
    /// instead of being retained.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> is <see langword="null"/>.
    /// </exception>
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
    /// Configures the component so that every dependent component or manual
    /// resolve within a single <see cref="ILifetimeScope"/> shares one instance
    /// taken from a pool governed by a policy from the supplied factory,
    /// returning it to the pool when the scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="policyFactory">
    /// A factory that returns the policy to use, invoked when the pool is
    /// built.
    /// </param>
    /// <remarks>
    /// <para>
    /// The factory is invoked with the current <see cref="IComponentContext"/>,
    /// so the policy can be resolved as a component and have its dependencies
    /// managed by the container (for example,
    /// <c>ctx =&gt; ctx.Resolve&lt;IMyPolicy&gt;()</c>).
    /// </para>
    /// <para>
    /// The pool retains up to the
    /// <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/> value
    /// returned by the factory. Instances requested beyond that are disposed or
    /// discarded instead of being retained.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> or
    /// <paramref name="policyFactory"/> is <see langword="null"/>.
    /// </exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> policyFactory)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        if (policyFactory == null)
        {
            throw new ArgumentNullException(nameof(policyFactory));
        }

        RegisterPooled(registration, policyFactory, null, null);

        return registration;
    }

    /// <summary>
    /// Configures the component so that every dependent component or manual
    /// resolve within a single <see cref="ILifetimeScope"/> shares one instance
    /// taken from a pool whose storage and eviction are controlled by a custom
    /// <see cref="ObjectPoolProvider"/>, returning it to the pool when the
    /// scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="providerFactory">
    /// A factory that returns the <see cref="ObjectPoolProvider"/> that creates
    /// the backing pool, invoked once when the pool is built.
    /// </param>
    /// <remarks>
    /// <para>
    /// The factory is invoked once, resolved from the pool-owning (root) scope,
    /// so it can resolve dependencies from the <see cref="IComponentContext"/>
    /// (for example, <c>ctx =&gt; ctx.Resolve&lt;ObjectPoolProvider&gt;()</c>).
    /// Autofac still owns construction of the pooled instances and the pooling
    /// callbacks; the provider only controls where instances are stored and
    /// when they are evicted.
    /// </para>
    /// <para>
    /// Because the provider owns sizing and eviction,
    /// <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/> does not
    /// size the pool. The pool must be thread-safe, because it is shared
    /// across all lifetime scopes and threads.
    /// </para>
    /// <para>
    /// If the pool implements <see cref="IDisposable"/>, the container disposes
    /// it at shutdown. Because <see cref="ObjectPool{T}.Return(T)"/> reports no
    /// result and there is no eviction callback, the pool is responsible for
    /// disposing instances it declines on return or evicts asynchronously.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> or
    /// <paramref name="providerFactory"/> is <see langword="null"/>.
    /// </exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               Func<IComponentContext, ObjectPoolProvider> providerFactory)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        if (providerFactory == null)
        {
            throw new ArgumentNullException(nameof(providerFactory));
        }

        RegisterPooled(registration, DefaultPolicyFactory<TLimit>.Instance, providerFactory, null);

        return registration;
    }

    /// <summary>
    /// Configures the component so that every dependent component or manual
    /// resolve within a single <see cref="ILifetimeScope"/> shares one instance
    /// taken from a pool whose behavior is controlled by a custom policy and
    /// whose storage and eviction are controlled by a custom
    /// <see cref="ObjectPoolProvider"/>, returning it to the pool when the
    /// scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="policyFactory">
    /// A factory that returns the policy to use, invoked when the pool is
    /// built.
    /// </param>
    /// <param name="providerFactory">
    /// A factory that returns the <see cref="ObjectPoolProvider"/> that creates
    /// the backing pool, invoked once when the pool is built.
    /// </param>
    /// <remarks>
    /// <para>
    /// Both factories are invoked once, resolved from the pool-owning (root)
    /// scope, so they can resolve dependencies from the
    /// <see cref="IComponentContext"/>. The policy controls how instances are
    /// retrieved from and returned to the pool; the provider controls where
    /// instances are stored and when they are evicted. Autofac still owns
    /// construction of the pooled instances and the pooling callbacks.
    /// </para>
    /// <para>
    /// Because the provider owns sizing and eviction,
    /// <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/> does not
    /// size the pool. The pool must be thread-safe, because it is shared
    /// across all lifetime scopes and threads.
    /// </para>
    /// <para>
    /// If the pool implements <see cref="IDisposable"/>, the container disposes
    /// it at shutdown. Because <see cref="ObjectPool{T}.Return(T)"/> reports no
    /// result and there is no eviction callback, the pool is responsible for
    /// disposing instances it declines on return or evicts asynchronously.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/>,
    /// <paramref name="policyFactory"/>, or
    /// <paramref name="providerFactory"/> is <see langword="null"/>.
    /// </exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> policyFactory,
               Func<IComponentContext, ObjectPoolProvider> providerFactory)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        if (policyFactory == null)
        {
            throw new ArgumentNullException(nameof(policyFactory));
        }

        if (providerFactory == null)
        {
            throw new ArgumentNullException(nameof(providerFactory));
        }

        RegisterPooled(registration, policyFactory, providerFactory, null);

        return registration;
    }

    /// <summary>
    /// Configures the component so that every dependent component or manual
    /// resolve within a <see cref="ILifetimeScope"/> tagged with any of these
    /// tags shares one instance taken from a pool, returning it to the pool
    /// when the scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="lifetimeScopeTags">
    /// The tags identifying the matching lifetime scopes.
    /// </param>
    /// <remarks>
    /// <para>
    /// Dependent components in scopes nested below a matching scope share that
    /// scope's instance.
    /// </para>
    /// <para>
    /// The pool retains up to twice the processor count
    /// (<see cref="Environment.ProcessorCount"/> x 2) instances. Instances
    /// requested beyond that are disposed or discarded instead of being
    /// retained.
    /// </para>
    /// <para>
    /// To run behavior when an instance is taken from or returned to the pool,
    /// implement <see cref="IPooledComponent"/> on the component, or use an
    /// overload that accepts a custom
    /// <see cref="IPooledRegistrationPolicy{TLimit}"/>.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DependencyResolutionException">
    /// Thrown at resolve time when no scope tagged with one of
    /// <paramref name="lifetimeScopeTags"/> exists in the hierarchy.
    /// </exception>
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
    /// Configures the component so that every dependent component or manual
    /// resolve within a <see cref="ILifetimeScope"/> tagged with any of these
    /// tags shares one instance taken from a pool, returning it to the pool
    /// when the scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="maximumRetainedInstances">
    /// The maximum number of instances to retain in the pool.
    /// </param>
    /// <param name="lifetimeScopeTags">
    /// The tags identifying the matching lifetime scopes.
    /// </param>
    /// <remarks>
    /// <para>
    /// Dependent components in scopes nested below a matching scope share that
    /// scope's instance.
    /// </para>
    /// <para>
    /// The pool retains up to <paramref name="maximumRetainedInstances"/>
    /// instances. Instances requested beyond that are disposed or discarded
    /// instead of being retained.
    /// </para>
    /// <para>
    /// To run behavior when an instance is taken from or returned to the pool,
    /// implement <see cref="IPooledComponent"/> on the component, or use an
    /// overload that accepts a custom
    /// <see cref="IPooledRegistrationPolicy{TLimit}"/>.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DependencyResolutionException">
    /// Thrown at resolve time when no scope tagged with one of
    /// <paramref name="lifetimeScopeTags"/> exists in the hierarchy.
    /// </exception>
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
    /// Configures the component so that every dependent component or manual
    /// resolve within a <see cref="ILifetimeScope"/> tagged with any of these
    /// tags shares one instance taken from a pool governed by a custom policy,
    /// returning it to the pool when the scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="poolPolicy">
    /// A custom policy that controls pool behavior.
    /// </param>
    /// <param name="lifetimeScopeTags">
    /// The tags identifying the matching lifetime scopes.
    /// </param>
    /// <remarks>
    /// <para>
    /// Dependent components in scopes nested below a matching scope share that
    /// scope's instance. The policy gives fine-grained control over how
    /// instances are retrieved from the pool, including whether an instance is
    /// returned to the pool at all.
    /// </para>
    /// <para>
    /// The pool retains up to
    /// <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/>
    /// instances. Instances requested beyond that are disposed or discarded
    /// instead of being retained.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DependencyResolutionException">
    /// Thrown at resolve time when no scope tagged with one of
    /// <paramref name="lifetimeScopeTags"/> exists in the hierarchy.
    /// </exception>
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

    /// <summary>
    /// Configures the component so that every dependent component or manual
    /// resolve within a <see cref="ILifetimeScope"/> tagged with any of these
    /// tags shares one instance taken from a pool governed by a policy from the
    /// supplied factory, returning it to the pool when the scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="policyFactory">
    /// A factory that returns the policy to use, invoked when the pool is
    /// built.
    /// </param>
    /// <param name="lifetimeScopeTags">
    /// The tags identifying the matching lifetime scopes.
    /// </param>
    /// <remarks>
    /// <para>
    /// Dependent components in scopes nested below a matching scope share that
    /// scope's instance. The factory is invoked with the current
    /// <see cref="IComponentContext"/>, so the policy can be resolved as a
    /// component and have its dependencies managed by the container (for
    /// example, <c>ctx =&gt; ctx.Resolve&lt;IMyPolicy&gt;()</c>).
    /// </para>
    /// <para>
    /// The pool retains up to the
    /// <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/> value
    /// returned by the factory. Instances requested beyond that are disposed or
    /// discarded instead of being retained.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> or
    /// <paramref name="policyFactory"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DependencyResolutionException">
    /// Thrown at resolve time when no scope tagged with one of
    /// <paramref name="lifetimeScopeTags"/> exists in the hierarchy.
    /// </exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerMatchingLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> policyFactory,
               params object[] lifetimeScopeTags)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        if (policyFactory == null)
        {
            throw new ArgumentNullException(nameof(policyFactory));
        }

        RegisterPooled(registration, policyFactory, null, lifetimeScopeTags);

        return registration;
    }

    /// <summary>
    /// Configures the component so that every dependent component or manual
    /// resolve within a <see cref="ILifetimeScope"/> tagged with any of these
    /// tags shares one instance taken from a pool whose storage and eviction
    /// are controlled by a custom <see cref="ObjectPoolProvider"/>, returning
    /// it to the pool when the scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="providerFactory">
    /// A factory that returns the <see cref="ObjectPoolProvider"/> that creates
    /// the backing pool, invoked once when the pool is built.
    /// </param>
    /// <param name="lifetimeScopeTags">
    /// The tags identifying the matching lifetime scopes.
    /// </param>
    /// <remarks>
    /// <para>
    /// Dependent components in scopes nested below a matching scope share that
    /// scope's instance. The factory is invoked once, resolved from the
    /// pool-owning (root) scope. Autofac still owns construction of the pooled
    /// instances and the pooling callbacks; the provider only controls where
    /// instances are stored and when they are evicted.
    /// </para>
    /// <para>
    /// Because the provider owns sizing and eviction,
    /// <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/> does not
    /// size the pool. The pool must be thread-safe, because it is shared
    /// across all lifetime scopes and threads.
    /// </para>
    /// <para>
    /// If the pool implements <see cref="IDisposable"/>, the container disposes
    /// it at shutdown. Because <see cref="ObjectPool{T}.Return(T)"/> reports no
    /// result and there is no eviction callback, the pool is responsible for
    /// disposing instances it declines on return or evicts asynchronously.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/> or
    /// <paramref name="providerFactory"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DependencyResolutionException">
    /// Thrown at resolve time when no scope tagged with one of
    /// <paramref name="lifetimeScopeTags"/> exists in the hierarchy.
    /// </exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerMatchingLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               Func<IComponentContext, ObjectPoolProvider> providerFactory,
               params object[] lifetimeScopeTags)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        if (providerFactory == null)
        {
            throw new ArgumentNullException(nameof(providerFactory));
        }

        RegisterPooled(registration, DefaultPolicyFactory<TLimit>.Instance, providerFactory, lifetimeScopeTags);

        return registration;
    }

    /// <summary>
    /// Configures the component so that every dependent component or manual
    /// resolve within a <see cref="ILifetimeScope"/> tagged with any of these
    /// tags shares one instance taken from a pool whose behavior is controlled
    /// by a custom policy and whose storage and eviction are controlled by a
    /// custom <see cref="ObjectPoolProvider"/>, returning it to the pool when
    /// the scope ends.
    /// </summary>
    /// <typeparam name="TLimit">
    /// The registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// The activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// The registration style.
    /// </typeparam>
    /// <param name="registration">
    /// The registration to configure.
    /// </param>
    /// <param name="policyFactory">
    /// A factory that returns the policy to use, invoked when the pool is
    /// built.
    /// </param>
    /// <param name="providerFactory">
    /// A factory that returns the <see cref="ObjectPoolProvider"/> that creates
    /// the backing pool, invoked once when the pool is built.
    /// </param>
    /// <param name="lifetimeScopeTags">
    /// The tags identifying the matching lifetime scopes.
    /// </param>
    /// <remarks>
    /// <para>
    /// Dependent components in scopes nested below a matching scope share that
    /// scope's instance. Both factories are invoked once, resolved from the
    /// pool-owning (root) scope. The policy controls how instances are
    /// retrieved from and returned to the pool; the provider controls where
    /// instances are stored and when they are evicted. Autofac still owns
    /// construction of the pooled instances and the pooling callbacks.
    /// </para>
    /// <para>
    /// Because the provider owns sizing and eviction,
    /// <see cref="IPooledRegistrationPolicy{TLimit}.MaximumRetained"/> does not
    /// size the pool. The pool must be thread-safe, because it is shared
    /// across all lifetime scopes and threads.
    /// </para>
    /// <para>
    /// If the pool implements <see cref="IDisposable"/>, the container disposes
    /// it at shutdown. Because <see cref="ObjectPool{T}.Return(T)"/> reports no
    /// result and there is no eviction callback, the pool is responsible for
    /// disposing instances it declines on return or evicts asynchronously.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The registration builder, to enable further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registration"/>,
    /// <paramref name="policyFactory"/>, or
    /// <paramref name="providerFactory"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DependencyResolutionException">
    /// Thrown at resolve time when no scope tagged with one of
    /// <paramref name="lifetimeScopeTags"/> exists in the hierarchy.
    /// </exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
           PooledInstancePerMatchingLifetimeScope<TLimit, TActivatorData, TSingleRegistrationStyle>(
               this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
               Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> policyFactory,
               Func<IComponentContext, ObjectPoolProvider> providerFactory,
               params object[] lifetimeScopeTags)
           where TSingleRegistrationStyle : SingleRegistrationStyle
           where TActivatorData : IConcreteActivatorData
           where TLimit : class
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        if (policyFactory == null)
        {
            throw new ArgumentNullException(nameof(policyFactory));
        }

        if (providerFactory == null)
        {
            throw new ArgumentNullException(nameof(providerFactory));
        }

        RegisterPooled(registration, policyFactory, providerFactory, lifetimeScopeTags);

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
        if (registrationPolicy == null)
        {
            throw new ArgumentNullException(nameof(registrationPolicy));
        }

        // A fixed policy is shared between the pool-build and get sides by
        // resolving the same instance every time.
        RegisterPooled(registration, _ => registrationPolicy, null, tags);
    }

    private static void RegisterPooled<TLimit, TActivatorData, TSingleRegistrationStyle>(
        IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
        Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> policyFactory,
        Func<IComponentContext, ObjectPoolProvider>? providerFactory,
        object[]? tags)
        where TSingleRegistrationStyle : SingleRegistrationStyle
        where TActivatorData : IConcreteActivatorData
        where TLimit : class
    {
        // registration and policyFactory are always validated by the public
        // overloads (and PoolActivator guards policyFactory again), so no null
        // checks are repeated here.

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
                new PoolActivator<TLimit>(pooledInstanceService, policyFactory, providerFactory),
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
                new PoolGetActivator<TLimit>(poolService),
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

    /// <summary>
    /// Holds a cached default policy factory per closed <typeparamref name="TLimit"/>,
    /// so the provider-only overloads do not allocate a new delegate per call.
    /// </summary>
    /// <typeparam name="TLimit">The registration limit type.</typeparam>
    private static class DefaultPolicyFactory<TLimit>
        where TLimit : class
    {
        public static readonly Func<IComponentContext, IPooledRegistrationPolicy<TLimit>> Instance =
            _ => new DefaultPooledRegistrationPolicy<TLimit>();
    }
}
