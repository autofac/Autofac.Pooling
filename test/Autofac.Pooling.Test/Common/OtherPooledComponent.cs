// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Pooling.Tests.Common;

public class OtherPooledComponent : IPooledService
{
    public int GetCalled => 0;

    public int ReturnCalled => 0;

    public int DisposeCalled => 0;
}
