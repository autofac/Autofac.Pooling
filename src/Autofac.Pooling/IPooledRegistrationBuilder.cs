// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Builder;
using Autofac.Core;

namespace Autofac.Pooling;

/// <summary>
/// A builder for configuring a pooled registration, returned by <see cref="RegistrationExtensions.PooledPerLifetimeScope{T limit, TActivatorData, TSingleRegistrationStyle}"/>.
/// </summary>
/// <typeparam name="TLimit">Registration limit type.</typeparam>
/// <typeparam name="TActivatorData">Activator data type.</typeparam>
/// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
public interface IPooledRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
    where TSingleRegistrationStyle : SingleRegistrationStyle
    where TActivatorData : IConcreteActivatorData
    where TLimit : class
{
    /// <summary>
    /// Gets the registration data for the pooled registration.
    /// </summary>
    RegistrationData RegistrationData { get; }
}
