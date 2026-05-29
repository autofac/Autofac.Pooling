namespace Autofac.Pooling.Tests.Common;

public class OtherPooledComponent : IPooledService
{
    public int GetCalled => 0;

    public int ReturnCalled => 0;

    public int DisposeCalled => 0;
}
