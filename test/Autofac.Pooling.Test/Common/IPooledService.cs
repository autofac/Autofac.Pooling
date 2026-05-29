namespace Autofac.Pooling.Tests.Common;

public interface IPooledService
{
    int GetCalled
    {
        get;
    }

    int ReturnCalled
    {
        get;
    }

    int DisposeCalled
    {
        get;
    }
}
