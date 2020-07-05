using Autofac.Core.Lifetime;

namespace Autofac.Pooling
{
    /// <summary>
    /// Lifetime wrapper to help us detect if we finished registering in a pooled configuration.
    /// </summary>
    public class PooledLifetime : CurrentScopeLifetime
    {
    }
}
