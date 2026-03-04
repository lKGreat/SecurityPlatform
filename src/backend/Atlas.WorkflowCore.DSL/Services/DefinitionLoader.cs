using Atlas.WorkflowCore.DSL.Interface;
using Atlas.WorkflowCore.DSL.Models.v1;
using Atlas.WorkflowCore.Exceptions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.DSL.Services;

/// <summary>
/// 工作流定义加载器
/// </summary>
public class DefinitionLoader : IDefinitionLoader
{
    private readonly ITypeResolver _typeResolver;
    private int _nextStepId = 0;

    public DefinitionLoader(ITypeResolver typeResolver)
    {
        _typeResolver = typeResolver;
    }

    public WorkflowDefinition LoadDefinitionFromJson(string json)
    {
        var source = Deserializers.DeserializeJson(json);
        return ConvertDefinition(source);
    }

    public WorkflowDefinition LoadDefinitionFromYaml(string yaml)
    {
        var source = Deserializers.DeserializeYaml(yaml);
        return ConvertDefinition(source);
    }

    public WorkflowDefinition LoadDefinitionFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"工作流定义文件不存在: {filePath}");
        }

        var content = File.ReadAllText(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".json" => LoadDefinitionFromJson(content),
            ".yaml" or ".yml" => LoadDefinitionFromYaml(content),
            _ => throw new WorkflowDefinitionLoadException($"不支持的文件格式: {extension}")
        };
    }

    private WorkflowDefinition ConvertDefinition(Models.DefinitionSource source)
    {
        _nextStepId = 0;

        if (source is not DefinitionSourceV1 v1Source)
        {
            throw new WorkflowDefinitionLoadException("不支持的定义版本");
        }

        var definition = new WorkflowDefinition
        {
            Id = v1Source.Id,
            Version = v1Source.Version,
            Description = v1Source.Description
        };

        // 解析数据类型
        if (!string.IsNullOrEmpty(v1Source.DataType))
        {
            definition.DataType = _typeResolver.ResolveType(v1Source.DataType);
            if (definition.DataType == null)
            {
                throw new WorkflowDefinitionLoadException($"无法解析数据类型: {v1Source.DataType}");
            }
        }

        // 解析默认错误行为
        if (!string.IsNullOrEmpty(v1Source.DefaultErrorBehavior))
        {
            if (Enum.TryParse<WorkflowErrorHandling>(v1Source.DefaultErrorBehavior, true, out var errorBehavior))
            {
                definition.DefaultErrorBehavior = errorBehavior;
            }
        }

        // 解析默认重试间隔
        if (v1Source.DefaultErrorRetryIntervalMs.HasValue)
        {
            definition.DefaultErrorRetryInterval = TimeSpan.FromMilliseconds(v1Source.DefaultErrorRetryIntervalMs.Value);
        }

        // 构建步骤
        var stepIdMap = new Dictionary<string, int>();
        foreach (var stepSource in v1Source.Steps)
        {
            var step = ConvertStep(stepSource, stepIdMap);
            definition.Steps.Add(step);
        }

        // 解析步骤之间的连接关系
        LinkSteps(v1Source.Steps, definition.Steps, stepIdMap);

        return definition;
    }

    private WorkflowStep ConvertStep(StepSourceV1 source, Dictionary<string, int> stepIdMap)
    {
        // 解析步骤类型
        if (string.IsNullOrEmpty(source.StepType))
        {
            throw new WorkflowDefinitionLoadException("步骤类型不能为空");
        }

        var stepType = _typeResolver.ResolveType(source.StepType);
        if (stepType == null)
        {
            throw new WorkflowDefinitionLoadException($"无法解析步骤类型: {source.StepType}");
        }

        // 创建步骤实例
        var step = (WorkflowStep?)Activator.CreateInstance(typeof(WorkflowStep<>).MakeGenericType(stepType));
        if (step == null)
        {
            throw new WorkflowDefinitionLoadException($"无法创建步骤实例: {source.StepType}");
        }

        step.Id = ++_nextStepId;
        step.Name = source.Name ?? stepType.Name;
        step.ExternalId = source.Id;

        // 记录步骤ID映射
        if (!string.IsNullOrEmpty(source.Id))
        {
            stepIdMap[source.Id] = step.Id;
        }

        // 解析错误行为
        if (!string.IsNullOrEmpty(source.OnError))
        {
            if (Enum.TryParse<WorkflowErrorHandling>(source.OnError, true, out var errorBehavior))
            {
                step.ErrorBehavior = errorBehavior;
            }
        }

        // 解析重试间隔
        if (source.RetryIntervalMs.HasValue)
        {
            step.RetryInterval = TimeSpan.FromMilliseconds(source.RetryIntervalMs.Value);
        }

        // 当前能力边界：暂不支持 DSL 输入输出映射表达式解析，避免引入运行时动态表达式依赖。
        // 跟踪任务：DSL-110（https://tracker.local/DSL-110），预计版本：v1.7。
        // 当前能力边界：暂不支持 DSL 子步骤块（Do）展开。
        // 跟踪任务：DSL-111（https://tracker.local/DSL-111），预计版本：v1.7。
        // 当前能力边界：暂不支持 DSL 补偿步骤绑定。
        // 跟踪任务：DSL-112（https://tracker.local/DSL-112），预计版本：v1.7。
        // 当前能力边界：暂不支持 DSL When 分支语义解析。
        // 跟踪任务：DSL-113（https://tracker.local/DSL-113），预计版本：v1.7。

        return step;
    }

    private void LinkSteps(List<StepSourceV1> sources, WorkflowStepCollection steps, Dictionary<string, int> stepIdMap)
    {
        var stepsList = steps.ToList();
        for (int i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            var step = stepsList[i];

            // 简单的顺序连接（NextStepId）
            if (!string.IsNullOrEmpty(source.NextStepId))
            {
                if (stepIdMap.TryGetValue(source.NextStepId, out var nextStepId))
                {
                    step.Outcomes.Add(new ValueOutcome { NextStep = nextStepId });
                }
                else
                {
                    throw new WorkflowDefinitionLoadException($"找不到下一步骤: {source.NextStepId}");
                }
            }

            // 当前能力边界：仅支持显式 NextStepId 连接；条件分支解析见任务 DSL-114（版本：v1.7）。
            // 当前能力边界：暂不支持 DSL When 分支连接。
            // 跟踪任务：DSL-113（https://tracker.local/DSL-113），预计版本：v1.7。
        }
    }
}
