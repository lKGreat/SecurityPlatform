using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.Services;
using Atlas.WorkflowCore.Services.BackgroundTasks;
using Atlas.WorkflowCore.Services.DefaultProviders;
using Atlas.WorkflowCore.Services.ErrorHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.WorkflowCore;

/// <summary>
/// 工作流引擎依赖注入扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加工作流引擎服务（使用默认配置）
    /// </summary>
    /// <remarks>
    /// 默认配置包括：
    /// - 内存持久化提供者（InMemoryPersistenceProvider）
    /// - 默认租户提供者（DefaultTenantProvider，返回空租户ID）
    /// - 默认工作流选项（1秒轮询间隔）
    /// </remarks>
    public static IServiceCollection AddWorkflowCore(this IServiceCollection services)
    {
        return services.AddWorkflowCore(builder => { });
    }

    /// <summary>
    /// 添加工作流引擎服务（自定义配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置构建器的委托</param>
    /// <example>
    /// 使用默认配置：
    /// <code>
    /// services.AddWorkflowCore();
    /// </code>
    /// 
    /// 使用内存持久化和自定义选项：
    /// <code>
    /// services.AddWorkflowCore(options => 
    /// {
    ///     options.UseInMemoryPersistence()
    ///            .ConfigureOptions(opts => opts.PollInterval = TimeSpan.FromSeconds(5));
    /// });
    /// </code>
    /// 
    /// 使用自定义租户提供者：
    /// <code>
    /// services.AddWorkflowCore(options => 
    /// {
    ///     options.UseTenantProvider&lt;MyTenantProvider&gt;();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddWorkflowCore(
        this IServiceCollection services,
        Action<WorkflowCoreBuilder> configure)
    {
        // 注册核心服务
        RegisterCoreServices(services);

        // 创建并配置 Builder
        var builder = new WorkflowCoreBuilder(services);
        configure(builder);
        builder.Build(); // 应用默认值并验证

        return services;
    }

    /// <summary>
    /// 注册 WorkflowCore 核心服务
    /// </summary>
    private static void RegisterCoreServices(IServiceCollection services)
    {
        // 核心服务
        services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowHost, WorkflowHost>();
        services.AddScoped<IWorkflowExecutor, WorkflowExecutor>();
        services.AddScoped<IStepExecutor, StepExecutor>();

        // 执行结果处理器和指针工厂
        services.AddScoped<IExecutionResultProcessor, ExecutionResultProcessor>();
        services.AddSingleton<IExecutionPointerFactory, ExecutionPointerFactory>();

        // 生命周期事件服务
        services.AddSingleton<ILifeCycleEventHub, LifeCycleEventHub>();
        services.AddSingleton<ILifeCycleEventPublisher, LifeCycleEventHub>(sp => 
            (LifeCycleEventHub)sp.GetRequiredService<ILifeCycleEventHub>());

        // 队列提供者（默认单节点实现）
        services.AddSingleton<IQueueProvider, SingleNodeQueueProvider>();

        // 锁提供者（默认单节点实现）
        services.AddSingleton<IDistributedLockProvider, SingleNodeLockProvider>();

        // 搜索索引（默认空实现）
        services.AddSingleton<ISearchIndex, NullSearchIndex>();

        // 辅助服务
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IScopeProvider, ScopeProvider>();
        services.AddSingleton<IGreyList, GreyList>();
        services.AddSingleton<IWorkflowPurger, WorkflowPurger>();

        // 取消处理器
        services.AddScoped<ICancellationProcessor, CancellationProcessor>();

        // 错误处理器
        services.AddSingleton<IWorkflowErrorHandler, RetryHandler>();
        services.AddSingleton<IWorkflowErrorHandler, SuspendHandler>();
        services.AddSingleton<IWorkflowErrorHandler, TerminateHandler>();
        services.AddSingleton<IWorkflowErrorHandler, CompensateHandler>();

        // 仓储接口（IPersistenceProvider 实现了所有仓储接口）
        services.AddSingleton<ISubscriptionRepository>(sp => 
            sp.GetRequiredService<IPersistenceProvider>());
        services.AddSingleton<IWorkflowRepository>(sp => 
            sp.GetRequiredService<IPersistenceProvider>());
        services.AddSingleton<IEventRepository>(sp => 
            sp.GetRequiredService<IPersistenceProvider>());
        services.AddSingleton<IScheduledCommandRepository>(sp => 
            sp.GetRequiredService<IPersistenceProvider>());

        // 活动控制器
        services.AddSingleton<IActivityController, ActivityController>();

        // 同步运行器
        services.AddScoped<ISyncWorkflowRunner, SyncWorkflowRunner>();

        // 中间件运行器
        services.AddScoped<IWorkflowMiddlewareRunner, WorkflowMiddlewareRunner>();
        services.AddSingleton<IWorkflowMiddlewareErrorHandler, DefaultWorkflowMiddlewareErrorHandler>();

        // 后台任务
        services.AddSingleton<IBackgroundTask, WorkflowConsumer>();
        services.AddSingleton<IBackgroundTask, EventConsumer>();
        services.AddSingleton<IBackgroundTask, IndexConsumer>();
        services.AddSingleton<IBackgroundTask, RunnablePoller>();
    }
}

