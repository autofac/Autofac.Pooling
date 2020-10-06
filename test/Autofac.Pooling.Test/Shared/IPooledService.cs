namespace Autofac.Pooling.Tests.Shared
{
    public interface IPooledService
    {
        int GetCalled { get; }

        int ReturnCalled { get; }

        int DisposeCalled { get; }
    }
}
