namespace Atlas.WorkflowCore.DSL.Interface;

/// <summary>
/// 类型解析器接口 - 根据类型名称解析为 .NET Type
/// </summary>
public interface ITypeResolver
{
    /// <summary>
    /// 解析类型名称为 Type 对象
    /// </summary>
    /// <param name="typeName">类型名称（支持完全限定名、部分名称）</param>
    /// <returns>解析后的 Type，如果无法解析则返回 null</returns>
    Type? ResolveType(string typeName);

    /// <summary>
    /// 注册类型别名
    /// </summary>
    /// <param name="alias">别名</param>
    /// <param name="type">对应的类型</param>
    void RegisterTypeAlias(string alias, Type type);

    /// <summary>
    /// 注册命名空间（用于简化类型名称解析）
    /// </summary>
    /// <param name="namespace">命名空间</param>
    void RegisterNamespace(string @namespace);
}
