using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services;

/// <summary>
/// 工作流中间件运行器实现
/// </summary>
public class WorkflowMiddlewareRunner : IWorkflowMiddlewareRunner
{
    private readonly IEnumerable<IWorkflowMiddleware> _middlewares;
    private readonly IWorkflowMiddlewareErrorHandler _errorHandler;
    private readonly ILogger<WorkflowMiddlewareRunner> _logger;

    public WorkflowMiddlewareRunner(
        IEnumerable<IWorkflowMiddleware> middlewares,
        IWorkflowMiddlewareErrorHandler errorHandler,
        ILogger<WorkflowMiddlewareRunner> logger)
    {
        _middlewares = middlewares;
        _errorHandler = errorHandler;
        _logger = logger;
    }

    public async Task RunPreMiddleware(WorkflowInstance workflow, CancellationToken cancellationToken = default)
    {
        foreach (var middleware in _middlewares)
        {
            try
            {
                await middleware.HandlePreWorkflow(workflow, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PreWorkflow 中间件执行失败: {MiddlewareType}", middleware.GetType().Name);
                await _errorHandler.HandleError(workflow, WorkflowMiddlewarePhase.PreWorkflow, ex, cancellationToken);
            }
        }
    }

    public async Task RunExecuteMiddleware(WorkflowInstance workflow, CancellationToken cancellationToken = default)
    {
        foreach (var middleware in _middlewares)
        {
            try
            {
                await middleware.HandleExecuteWorkflow(workflow, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecuteWorkflow 中间件执行失败: {MiddlewareType}", middleware.GetType().Name);
                await _errorHandler.HandleError(workflow, WorkflowMiddlewarePhase.ExecuteWorkflow, ex, cancellationToken);
            }
        }
    }

    public async Task RunPostMiddleware(WorkflowInstance workflow, CancellationToken cancellationToken = default)
    {
        foreach (var middleware in _middlewares)
        {
            try
            {
                await middleware.HandlePostWorkflow(workflow, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostWorkflow 中间件执行失败: {MiddlewareType}", middleware.GetType().Name);
                await _errorHandler.HandleError(workflow, WorkflowMiddlewarePhase.PostWorkflow, ex, cancellationToken);
            }
        }
    }
}
