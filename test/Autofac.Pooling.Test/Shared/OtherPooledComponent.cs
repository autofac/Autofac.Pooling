namespace Autofac.Pooling.Tests.Shared;

public class OtherPooledComponent : IPooledService
{
    public int GetCalled => 0;

    public int ReturnCalled => 0;

    public int DisposeCalled => 0;
}
