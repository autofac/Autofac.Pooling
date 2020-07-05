namespace Autofac.Pooling.Tests.Types
{
    public interface IPooledService
    {
        int GetCalled { get; }

        int ReturnCalled { get; }

        int DisposeCalled { get; }
    }
}
