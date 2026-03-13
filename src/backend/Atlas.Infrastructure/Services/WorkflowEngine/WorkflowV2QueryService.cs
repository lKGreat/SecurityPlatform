using Atlas.Application.Workflow.Abstractions.V2;
using Atlas.Application.Workflow.Models.V2;
using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Core.Models;
using Atlas.Domain.Workflow.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine;

public sealed class WorkflowV2QueryService : IWorkflowV2QueryService
{
    private readonly IWorkflowMetaRepository _metaRepo;
    private readonly IWorkflowDraftRepository _draftRepo;
    private readonly IWorkflowVersionRepository _versionRepo;
    private readonly IWorkflowExecutionRepository _executionRepo;
    private readonly INodeExecutionRepository _nodeExecutionRepo;

    public WorkflowV2QueryService(
        IWorkflowMetaRepository metaRepo,
        IWorkflowDraftRepository draftRepo,
        IWorkflowVersionRepository versionRepo,
        IWorkflowExecutionRepository executionRepo,
        INodeExecutionRepository nodeExecutionRepo)
    {
        _metaRepo = metaRepo;
        _draftRepo = draftRepo;
        _versionRepo = versionRepo;
        _executionRepo = executionRepo;
        _nodeExecutionRepo = nodeExecutionRepo;
    }

    public async Task<PagedResult<WorkflowListItem>> ListWorkflowsAsync(
        int pageIndex, int pageSize, string? keyword, CancellationToken cancellationToken)
    {
        var (items, total) = await _metaRepo.QueryPageAsync(pageIndex, pageSize, keyword, cancellationToken);

        var result = items.Select(m => new WorkflowListItem
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            Mode = m.Mode,
            Status = m.Status,
            LatestVersion = m.LatestVersion,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        }).ToList();

        return new PagedResult<WorkflowListItem>(result, total, pageIndex, pageSize);
    }

    public async Task<WorkflowDetailResponse?> GetWorkflowAsync(long workflowId, CancellationToken cancellationToken)
    {
        var meta = await _metaRepo.GetByIdAsync(workflowId, cancellationToken);
        if (meta is null) return null;

        var draft = await _draftRepo.GetByWorkflowIdAsync(workflowId, cancellationToken);

        return new WorkflowDetailResponse
        {
            Id = meta.Id,
            Name = meta.Name,
            Description = meta.Description,
            Mode = meta.Mode,
            Status = meta.Status,
            LatestVersion = meta.LatestVersion,
            CreatedAt = meta.CreatedAt,
            UpdatedAt = meta.UpdatedAt,
            CanvasJson = draft?.CanvasJson,
            CommitId = draft?.CommitId
        };
    }

    public async Task<IReadOnlyList<WorkflowVersionItem>> ListVersionsAsync(long workflowId, CancellationToken cancellationToken)
    {
        var versions = await _versionRepo.ListByWorkflowIdAsync(workflowId, cancellationToken);
        return versions.Select(v => new WorkflowVersionItem
        {
            Id = v.Id,
            Version = v.Version,
            CommitId = v.CommitId,
            ChangeLog = v.ChangeLog,
            PublishedAt = v.PublishedAt
        }).ToList();
    }

    public async Task<WorkflowProcessResponse?> GetExecutionProcessAsync(long executionId, CancellationToken cancellationToken)
    {
        var execution = await _executionRepo.GetByIdAsync(executionId, cancellationToken);
        if (execution is null) return null;

        var nodeExecutions = await _nodeExecutionRepo.ListByExecutionIdAsync(executionId, cancellationToken);

        return new WorkflowProcessResponse
        {
            ExecutionId = executionId,
            Status = execution.Status,
            CostMs = execution.CostMs,
            ErrorMessage = execution.ErrorMessage,
            Nodes = nodeExecutions.Select(n => new NodeExecutionItem
            {
                Id = n.Id,
                NodeKey = n.NodeKey,
                NodeType = n.NodeType,
                NodeTitle = n.NodeTitle,
                Status = n.Status,
                CostMs = n.CostMs,
                TokensUsed = n.TokensUsed,
                IterationIndex = n.IterationIndex,
                StartedAt = n.StartedAt,
                CompletedAt = n.CompletedAt
            }).ToList()
        };
    }

    public async Task<NodeExecutionDetailResponse?> GetNodeExecutionDetailAsync(long executionId, string nodeKey, CancellationToken cancellationToken)
    {
        var nodeExec = await _nodeExecutionRepo.GetByExecutionAndNodeKeyAsync(executionId, nodeKey, cancellationToken);
        if (nodeExec is null) return null;

        return new NodeExecutionDetailResponse
        {
            Id = nodeExec.Id,
            NodeKey = nodeExec.NodeKey,
            NodeType = nodeExec.NodeType,
            NodeTitle = nodeExec.NodeTitle,
            Status = nodeExec.Status,
            InputJson = nodeExec.InputJson,
            OutputJson = nodeExec.OutputJson,
            ErrorMessage = nodeExec.ErrorMessage,
            CostMs = nodeExec.CostMs,
            TokensUsed = nodeExec.TokensUsed,
            StartedAt = nodeExec.StartedAt,
            CompletedAt = nodeExec.CompletedAt
        };
    }

    public Task<IReadOnlyList<NodeTypeMetadata>> GetNodeTypesAsync(CancellationToken cancellationToken)
    {
        var types = new List<NodeTypeMetadata>
        {
            new() { Type = NodeType.Entry, Name = "开始", Description = "工作流入口，定义输入参数", Category = "基础", Icon = "play-circle" },
            new() { Type = NodeType.Exit, Name = "结束", Description = "工作流出口，定义输出参数", Category = "基础", Icon = "stop-circle" },
            new() { Type = NodeType.LLM, Name = "大模型", Description = "调用 LLM 进行文本生成", Category = "AI", Icon = "robot" },
            new() { Type = NodeType.If, Name = "条件判断", Description = "基于条件分支执行", Category = "流程控制", Icon = "git-branch" },
            new() { Type = NodeType.Loop, Name = "循环", Description = "遍历数组元素", Category = "流程控制", Icon = "repeat" },
            new() { Type = NodeType.Break, Name = "Break", Description = "退出循环", Category = "流程控制", Icon = "stop" },
            new() { Type = NodeType.Continue, Name = "Continue", Description = "跳过本次循环", Category = "流程控制", Icon = "skip-forward" },
            new() { Type = NodeType.Batch, Name = "批处理", Description = "并行处理数组元素", Category = "流程控制", Icon = "layers" },
            new() { Type = NodeType.SubWorkflow, Name = "子流程", Description = "调用已发布的子工作流", Category = "流程控制", Icon = "workflow" },
            new() { Type = NodeType.IntentDetector, Name = "意图识别", Description = "基于 LLM 的意图分类", Category = "AI", Icon = "target" },
            new() { Type = NodeType.KnowledgeRetriever, Name = "知识库检索", Description = "从知识库检索相关文档", Category = "AI/RAG", Icon = "book-open" },
            new() { Type = NodeType.KnowledgeIndexer, Name = "知识库写入", Description = "向知识库添加文档", Category = "AI/RAG", Icon = "book-plus" },
            new() { Type = NodeType.KnowledgeDeleter, Name = "知识库删除", Description = "从知识库删除文档", Category = "AI/RAG", Icon = "book-minus" },
            new() { Type = NodeType.CodeRunner, Name = "代码执行", Description = "执行代码片段", Category = "工具", Icon = "code" },
            new() { Type = NodeType.HttpRequester, Name = "HTTP 请求", Description = "发送 HTTP 请求", Category = "工具", Icon = "globe" },
            new() { Type = NodeType.PluginApi, Name = "插件调用", Description = "调用插件 API", Category = "工具", Icon = "puzzle" },
            new() { Type = NodeType.DatabaseQuery, Name = "数据库查询", Description = "执行 SQL 查询", Category = "数据", Icon = "database" },
            new() { Type = NodeType.DatabaseInsert, Name = "数据库插入", Description = "执行 SQL 插入", Category = "数据", Icon = "database-plus" },
            new() { Type = NodeType.DatabaseUpdate, Name = "数据库更新", Description = "执行 SQL 更新", Category = "数据", Icon = "database-edit" },
            new() { Type = NodeType.DatabaseDelete, Name = "数据库删除", Description = "执行 SQL 删除", Category = "数据", Icon = "database-minus" },
            new() { Type = NodeType.AssignVariable, Name = "变量赋值", Description = "给变量赋值", Category = "变量", Icon = "variable" },
            new() { Type = NodeType.VariableAggregator, Name = "变量聚合", Description = "合并多个变量", Category = "变量", Icon = "merge" },
            new() { Type = NodeType.JsonSerialization, Name = "JSON 序列化", Description = "对象转 JSON 字符串", Category = "JSON", Icon = "braces" },
            new() { Type = NodeType.JsonDeserialization, Name = "JSON 反序列化", Description = "JSON 字符串转对象", Category = "JSON", Icon = "unbraces" },
            new() { Type = NodeType.TextProcessor, Name = "文本处理", Description = "文本拼接/截取/格式化", Category = "文本", Icon = "type" },
            new() { Type = NodeType.MessageList, Name = "消息列表", Description = "读取会话消息", Category = "消息", Icon = "message-square" },
            new() { Type = NodeType.CreateMessage, Name = "创建消息", Description = "创建新消息", Category = "消息", Icon = "message-plus" },
            new() { Type = NodeType.ConversationList, Name = "会话列表", Description = "列出会话", Category = "消息", Icon = "messages-square" },
            new() { Type = NodeType.QuestionAnswer, Name = "提问等待", Description = "向用户提问并等待回答", Category = "交互", Icon = "help-circle" },
            new() { Type = NodeType.OutputEmitter, Name = "流式输出", Description = "流式输出文本片段", Category = "基础", Icon = "send" }
        };

        return Task.FromResult<IReadOnlyList<NodeTypeMetadata>>(types);
    }
}
