// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core.Lifetime;

namespace Autofac.Pooling;

/// <summary>
/// Lifetime wrapper to help us detect if we finished registering in a pooled configuration.
/// </summary>
[SuppressMessage("S2094", "S2094", Justification = "Lifetimes are classes, not interfaces. Changing this would be a breaking change.")]
public class PooledLifetime : CurrentScopeLifetime
{
}
