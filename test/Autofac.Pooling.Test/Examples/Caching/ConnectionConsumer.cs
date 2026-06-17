// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Pooling.Test.Examples.Caching;

/// <summary>
/// A component that depends on <see cref="ICustomConnection"/>, used to show that
/// a pooled instance is injected into a consumer like any other dependency.
/// </summary>
public sealed class ConnectionConsumer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionConsumer"/> class.
    /// </summary>
    /// <param name="connection">The pooled connection injected by Autofac.</param>
    public ConnectionConsumer(ICustomConnection connection)
    {
        Connection = connection;
    }

    /// <summary>
    /// Gets the pooled connection that was injected.
    /// </summary>
    public ICustomConnection Connection
    {
        get;
    }
}
