namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 容器步骤构建器接口 - 用于构建包含子步骤的容器（If、While、Foreach等）
/// </summary>
/// <typeparam name="TData">工作流数据类型</typeparam>
/// <typeparam name="TReturnStep">返回的步骤构建器类型</typeparam>
public interface IContainerStepBuilder<TData, TReturnStep>
{
    /// <summary>
    /// 定义容器内的步骤块
    /// </summary>
    /// <param name="builder">步骤构建器委托</param>
    /// <returns>返回步骤构建器</returns>
    TReturnStep Do(Action<IWorkflowBuilder<TData>> builder);
}
