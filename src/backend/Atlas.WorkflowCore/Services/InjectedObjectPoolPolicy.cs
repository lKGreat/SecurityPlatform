using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace Atlas.WorkflowCore.Services;

/// <summary>
/// 注入对象池策略 - 从DI容器创建对象实例
/// </summary>
internal class InjectedObjectPoolPolicy<T> : IPooledObjectPolicy<T> where T : class
{
    private readonly IServiceProvider _provider;

    public InjectedObjectPoolPolicy(IServiceProvider provider)
    {
        _provider = provider;
    }

    public T Create()
    {
        return _provider.GetRequiredService<T>();
    }

    public bool Return(T obj)
    {
        return true;
    }
}
