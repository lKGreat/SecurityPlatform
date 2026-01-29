using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.DSL.Interface;

/// <summary>
/// 工作流定义加载器接口
/// </summary>
public interface IDefinitionLoader
{
    /// <summary>
    /// 从 JSON 字符串加载工作流定义
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <returns>工作流定义</returns>
    WorkflowDefinition LoadDefinitionFromJson(string json);

    /// <summary>
    /// 从 YAML 字符串加载工作流定义
    /// </summary>
    /// <param name="yaml">YAML 字符串</param>
    /// <returns>工作流定义</returns>
    WorkflowDefinition LoadDefinitionFromYaml(string yaml);

    /// <summary>
    /// 从文件加载工作流定义（根据扩展名自动识别格式）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>工作流定义</returns>
    WorkflowDefinition LoadDefinitionFromFile(string filePath);
}
