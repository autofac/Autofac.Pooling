// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;

namespace Autofac.Pooling.Test.Examples.Caching;

/// <summary>
/// A connection-like service that is expensive enough to be worth pooling.
/// </summary>
public interface ICustomConnection
{
    /// <summary>
    /// Gets a stable identifier for the underlying instance, used by the tests
    /// to tell whether the same pooled instance was handed out again.
    /// </summary>
    int InstanceId
    {
        get;
    }

    /// <summary>
    /// Gets the number of times this instance has been taken from the pool.
    /// </summary>
    int GetFromPoolCount
    {
        get;
    }

    /// <summary>
    /// Does some representative work.
    /// </summary>
    /// <returns>A result derived from the work.</returns>
    string DoSomething();
}
