using Atlas.WorkflowCore.DSL.Models;
using Atlas.WorkflowCore.DSL.Models.v1;
using Atlas.WorkflowCore.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Atlas.WorkflowCore.DSL.Services;

/// <summary>
/// DSL 反序列化器
/// </summary>
public static class Deserializers
{
    /// <summary>
    /// 从 JSON 字符串反序列化工作流定义
    /// </summary>
    public static DefinitionSource DeserializeJson(string json)
    {
        try
        {
            var envelope = JsonConvert.DeserializeObject<JObject>(json);
            if (envelope == null)
            {
                throw new WorkflowDefinitionLoadException("无法解析 JSON 内容");
            }

            // 获取版本号
            var version = envelope["Version"]?.Value<int>() ?? 1;

            // 根据版本反序列化
            return version switch
            {
                1 => DeserializeV1FromJson(json),
                _ => throw new WorkflowDefinitionLoadException($"不支持的 DSL 版本: {version}")
            };
        }
        catch (JsonException ex)
        {
            throw new WorkflowDefinitionLoadException($"JSON 反序列化失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从 YAML 字符串反序列化工作流定义
    /// </summary>
    public static DefinitionSource DeserializeYaml(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var envelope = deserializer.Deserialize<Dictionary<string, object>>(yaml);
            if (envelope == null)
            {
                throw new WorkflowDefinitionLoadException("无法解析 YAML 内容");
            }

            // 获取版本号
            var version = envelope.ContainsKey("version") 
                ? Convert.ToInt32(envelope["version"]) 
                : 1;

            // 根据版本反序列化
            return version switch
            {
                1 => DeserializeV1FromYaml(yaml),
                _ => throw new WorkflowDefinitionLoadException($"不支持的 DSL 版本: {version}")
            };
        }
        catch (Exception ex) when (ex is not WorkflowDefinitionLoadException)
        {
            throw new WorkflowDefinitionLoadException($"YAML 反序列化失败: {ex.Message}", ex);
        }
    }

    private static DefinitionSourceV1 DeserializeV1FromJson(string json)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        var definition = JsonConvert.DeserializeObject<DefinitionSourceV1>(json, settings);
        if (definition == null)
        {
            throw new WorkflowDefinitionLoadException("无法反序列化 V1 定义");
        }

        return definition;
    }

    private static DefinitionSourceV1 DeserializeV1FromYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var definition = deserializer.Deserialize<DefinitionSourceV1>(yaml);
        if (definition == null)
        {
            throw new WorkflowDefinitionLoadException("无法反序列化 V1 定义");
        }

        return definition;
    }
}
