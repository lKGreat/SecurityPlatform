using Atlas.Application.Visualization.Abstractions;
using Atlas.Application.Workflow.Abstractions;
using Atlas.Application.Workflow.Abstractions.V2;
using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Infrastructure.Repositories.Workflow;
using Atlas.Infrastructure.Services;
using Atlas.Infrastructure.Services.Visualization;
using Atlas.Infrastructure.Services.WorkflowEngine;
using Atlas.Infrastructure.Services.WorkflowEngine.Nodes;
using Atlas.Infrastructure.Workflow;
using Atlas.WorkflowCore.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Infrastructure.DependencyInjection;

/// <summary>
/// Registers workflow and visualization services.
/// </summary>
public static class WorkflowServiceRegistration
{
    public static IServiceCollection AddWorkflowInfrastructure(this IServiceCollection services)
    {
        // === 原有 WorkflowCore 服务（保留） ===
        services.AddScoped<IPersistenceProvider, SqlSugarPersistenceProvider>();
        services.AddScoped<IWorkflowQueryService, WorkflowQueryService>();
        services.AddScoped<IWorkflowCommandService, WorkflowCommandService>();
        services.AddScoped<IVisualizationQueryService, VisualizationQueryService>();

        // === Coze 风格 DAG 工作流引擎 (v2) ===

        // Repositories
        services.AddScoped<IWorkflowMetaRepository, WorkflowMetaRepository>();
        services.AddScoped<IWorkflowDraftRepository, WorkflowDraftRepository>();
        services.AddScoped<IWorkflowVersionRepository, WorkflowVersionRepository>();
        services.AddScoped<IWorkflowExecutionRepository, WorkflowExecutionRepository>();
        services.AddScoped<INodeExecutionRepository, NodeExecutionRepository>();

        // Node Executors
        services.AddScoped<INodeExecutor, EntryNodeExecutor>();
        services.AddScoped<INodeExecutor, ExitNodeExecutor>();
        services.AddScoped<INodeExecutor, IfNodeExecutor>();
        services.AddScoped<INodeExecutor, LoopNodeExecutor>();
        services.AddScoped<INodeExecutor, BatchNodeExecutor>();
        services.AddScoped<INodeExecutor, AssignVariableNodeExecutor>();
        services.AddScoped<INodeExecutor, VariableAggregatorNodeExecutor>();
        services.AddScoped<INodeExecutor, LlmNodeExecutor>();
        services.AddScoped<INodeExecutor, IntentDetectorNodeExecutor>();
        services.AddScoped<INodeExecutor, KnowledgeRetrieverNodeExecutor>();
        services.AddScoped<INodeExecutor, HttpRequesterNodeExecutor>();
        services.AddScoped<INodeExecutor, CodeRunnerNodeExecutor>();
        services.AddScoped<INodeExecutor, JsonSerializationNodeExecutor>();
        services.AddScoped<INodeExecutor, JsonDeserializationNodeExecutor>();
        services.AddScoped<INodeExecutor, TextProcessorNodeExecutor>();
        services.AddScoped<INodeExecutor, OutputEmitterNodeExecutor>();
        services.AddScoped<INodeExecutor, QuestionAnswerNodeExecutor>();
        services.AddScoped<INodeExecutor, DatabaseQueryNodeExecutor>();
        services.AddScoped<INodeExecutor, MessageListNodeExecutor>();
        services.AddScoped<INodeExecutor, CreateMessageNodeExecutor>();
        services.AddScoped<INodeExecutor, ConversationListNodeExecutor>();

        // Node Registry
        services.AddScoped<NodeExecutorRegistry>(sp =>
        {
            var executors = sp.GetServices<INodeExecutor>();
            return new NodeExecutorRegistry(executors);
        });

        // DAG Engine & Application Services
        services.AddScoped<DagExecutor>();
        services.AddScoped<IWorkflowV2CommandService, WorkflowV2CommandService>();
        services.AddScoped<IWorkflowV2QueryService, WorkflowV2QueryService>();
        services.AddScoped<IWorkflowV2ExecutionService, WorkflowV2ExecutionService>();

        // SubWorkflow node needs DagExecutor (lazy to avoid circular DI)
        services.AddScoped<SubWorkflowNodeExecutor>();

        // HTTP client for HttpRequester node
        services.AddHttpClient("WorkflowHttp")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(60));

        return services;
    }
}
