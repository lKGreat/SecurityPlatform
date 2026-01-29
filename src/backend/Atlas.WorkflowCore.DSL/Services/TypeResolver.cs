using System.Reflection;
using Atlas.WorkflowCore.DSL.Interface;

namespace Atlas.WorkflowCore.DSL.Services;

/// <summary>
/// 类型解析器 - 根据类型名称解析为 .NET Type
/// </summary>
public class TypeResolver : ITypeResolver
{
    private readonly Dictionary<string, Type> _typeAliases = new();
    private readonly List<string> _namespaces = new();
    private readonly List<Assembly> _assemblies = new();

    public TypeResolver()
    {
        // 默认注册当前应用域中已加载的程序集
        _assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());

        // 默认注册常用命名空间
        RegisterNamespace("Atlas.WorkflowCore.Primitives");
        RegisterNamespace("System");
    }

    public Type? ResolveType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        // 1. 检查别名
        if (_typeAliases.TryGetValue(typeName, out var aliasType))
        {
            return aliasType;
        }

        // 2. 尝试直接解析（完全限定名）
        var type = Type.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        // 3. 在已注册的命名空间中查找
        foreach (var ns in _namespaces)
        {
            var fullName = $"{ns}.{typeName}";
            type = Type.GetType(fullName);
            if (type != null)
            {
                return type;
            }

            // 在已加载的程序集中查找
            foreach (var assembly in _assemblies)
            {
                type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }
        }

        // 4. 在所有已加载的程序集中查找（不带命名空间的类型名）
        foreach (var assembly in _assemblies)
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            // 查找所有导出类型
            try
            {
                type = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
                if (type != null)
                {
                    return type;
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 某些程序集可能无法加载所有类型，忽略错误继续
            }
        }

        return null;
    }

    public void RegisterTypeAlias(string alias, Type type)
    {
        _typeAliases[alias] = type;
    }

    public void RegisterNamespace(string @namespace)
    {
        if (!_namespaces.Contains(@namespace))
        {
            _namespaces.Add(@namespace);
        }
    }

    /// <summary>
    /// 注册程序集（用于类型查找）
    /// </summary>
    public void RegisterAssembly(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
        {
            _assemblies.Add(assembly);
        }
    }
}
