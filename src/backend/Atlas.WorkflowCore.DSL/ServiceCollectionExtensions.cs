using Atlas.WorkflowCore.DSL.Interface;
using Atlas.WorkflowCore.DSL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.WorkflowCore.DSL;

/// <summary>
/// DSL 服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 WorkflowCore DSL 支持
    /// </summary>
    public static IServiceCollection AddWorkflowCoreDsl(this IServiceCollection services)
    {
        // 注册类型解析器
        services.AddSingleton<ITypeResolver, TypeResolver>();

        // 注册定义加载器
        services.AddSingleton<IDefinitionLoader, DefinitionLoader>();

        return services;
    }

    /// <summary>
    /// 添加 WorkflowCore DSL 支持（带配置）
    /// </summary>
    public static IServiceCollection AddWorkflowCoreDsl(
        this IServiceCollection services, 
        Action<DslOptions> configure)
    {
        var options = new DslOptions();
        configure(options);

        // 注册类型解析器
        var typeResolver = new TypeResolver();

        // 应用配置
        foreach (var ns in options.Namespaces)
        {
            typeResolver.RegisterNamespace(ns);
        }

        foreach (var alias in options.TypeAliases)
        {
            typeResolver.RegisterTypeAlias(alias.Key, alias.Value);
        }

        services.AddSingleton<ITypeResolver>(typeResolver);

        // 注册定义加载器
        services.AddSingleton<IDefinitionLoader, DefinitionLoader>();

        return services;
    }
}

/// <summary>
/// DSL 配置选项
/// </summary>
public class DslOptions
{
    /// <summary>
    /// 注册的命名空间列表
    /// </summary>
    public List<string> Namespaces { get; } = new();

    /// <summary>
    /// 类型别名映射
    /// </summary>
    public Dictionary<string, Type> TypeAliases { get; } = new();

    /// <summary>
    /// 注册命名空间
    /// </summary>
    public DslOptions AddNamespace(string @namespace)
    {
        Namespaces.Add(@namespace);
        return this;
    }

    /// <summary>
    /// 注册类型别名
    /// </summary>
    public DslOptions AddTypeAlias(string alias, Type type)
    {
        TypeAliases[alias] = type;
        return this;
    }
}
