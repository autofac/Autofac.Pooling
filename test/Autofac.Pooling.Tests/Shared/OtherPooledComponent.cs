namespace Autofac.Pooling.Tests.Shared
{
    public class OtherPooledComponent : IPooledService
    {
        public int GetCalled => throw new System.NotImplementedException();

        public int ReturnCalled => throw new System.NotImplementedException();

        public int DisposeCalled => throw new System.NotImplementedException();
    }
}
