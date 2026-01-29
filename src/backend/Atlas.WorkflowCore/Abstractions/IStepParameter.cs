namespace Atlas.WorkflowCore.Abstractions;

public interface IStepParameter
{
    /// <summary>
    /// 解析参数值（向后兼容方法）
    /// </summary>
    object? Resolve(object? data);

    /// <summary>
    /// 将数据对象的属性映射到步骤体的属性（输入映射）
    /// </summary>
    void AssignInput(object data, IStepBody body, IStepExecutionContext context);

    /// <summary>
    /// 将步骤体的属性映射到数据对象的属性（输出映射）
    /// </summary>
    void AssignOutput(object data, IStepBody body, IStepExecutionContext context);
}
