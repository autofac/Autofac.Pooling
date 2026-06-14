// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Autofac.Builder;
using Autofac.Core;

namespace Autofac.Pooling;

/// <summary>
/// Internal implementation of <see cref="IPooledRegistrationBuilder{TLimit, TActivatorData, TSingleRegistrationStyle}"/>.
/// </summary>
internal sealed class PooledRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
    : IPooledRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
    where TSingleRegistrationStyle : SingleRegistrationStyle
    where TActivatorData : IConcreteActivatorData
    where TLimit : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PooledRegistrationBuilder{TLimit, TActivatorData, TSingleRegistrationStyle}"/> class.
    /// </summary>
    /// <param name="registration">The underlying registration builder.</param>
    public PooledRegistrationBuilder(IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration)
    {
        if (registration is null) throw new ArgumentNullException(nameof(registration));
        RegistrationData = registration.RegistrationData;
    }

    /// <inheritdoc/>
    public RegistrationData RegistrationData { get; }
}
