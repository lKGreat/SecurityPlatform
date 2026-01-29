using System.Collections;

namespace Atlas.WorkflowCore.Models;

public class WorkflowDefinition
{
    public string Id { get; set; } = string.Empty;

    public int Version { get; set; }

    public string? Description { get; set; }

    public WorkflowStepCollection Steps { get; set; } = new();

    public Type? DataType { get; set; }

    public WorkflowErrorHandling DefaultErrorBehavior { get; set; }

    public Type? OnPostMiddlewareError { get; set; }

    public Type? OnExecuteMiddlewareError { get; set; }

    public TimeSpan? DefaultErrorRetryInterval { get; set; }
}

public class WorkflowStepCollection : ICollection<WorkflowStep>
{
    private readonly Dictionary<int, WorkflowStep> _dictionary = new Dictionary<int, WorkflowStep>();
    
    public WorkflowStepCollection()
    {
    }

    public WorkflowStepCollection(int capacity)
    {
        _dictionary = new Dictionary<int, WorkflowStep>(capacity);
    }

    public WorkflowStepCollection(ICollection<WorkflowStep> steps)
    {
        foreach (var step in steps)
        {
            Add(step);
        }
    }

    public IEnumerator<WorkflowStep> GetEnumerator()
    {
        return _dictionary.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public WorkflowStep? FindById(int id)
    {
        if (!_dictionary.ContainsKey(id))
            return null;

        return _dictionary[id];
    }
    
    public void Add(WorkflowStep item)
    {
        _dictionary.Add(item.Id, item);
    }

    public void Clear()
    {
        _dictionary.Clear();
    }

    public bool Contains(WorkflowStep item)
    {
        return _dictionary.ContainsValue(item);
    }

    public void CopyTo(WorkflowStep[] array, int arrayIndex)
    {
        _dictionary.Values.CopyTo(array, arrayIndex);
    }

    public bool Remove(WorkflowStep item)
    {
        return _dictionary.Remove(item.Id);
    }

    public WorkflowStep? Find(Predicate<WorkflowStep> match)
    {
        return _dictionary.Values.FirstOrDefault(x => match(x));
    }

    public int Count => _dictionary.Count;
    public bool IsReadOnly => false;

    /// <summary>
    /// 根据外部ID查找步骤
    /// </summary>
    public WorkflowStep? FindByExternalId(string externalId)
    {
        return _dictionary.Values.FirstOrDefault(s => s.ExternalId == externalId);
    }

    /// <summary>
    /// 根据名称查找步骤
    /// </summary>
    public WorkflowStep? FindByName(string name)
    {
        return _dictionary.Values.FirstOrDefault(s => s.Name == name);
    }

    /// <summary>
    /// 根据名称查找所有匹配的步骤
    /// </summary>
    public List<WorkflowStep> FindAllByName(string name)
    {
        return _dictionary.Values.Where(s => s.Name == name).ToList();
    }

    /// <summary>
    /// 获取所有根步骤（没有前置步骤的步骤）
    /// </summary>
    public List<WorkflowStep> GetRootSteps()
    {
        var allNextStepIds = _dictionary.Values.SelectMany(s => s.Outcomes)
            .Select(o => o.NextStep)
            .Where(id => id > 0)
            .Distinct()
            .ToHashSet();

        return _dictionary.Values.Where(s => !allNextStepIds.Contains(s.Id)).ToList();
    }

    /// <summary>
    /// 获取指定步骤的所有子步骤
    /// </summary>
    public List<WorkflowStep> GetChildSteps(int parentStepId)
    {
        var parentStep = FindById(parentStepId);
        if (parentStep == null)
        {
            return new List<WorkflowStep>();
        }

        return parentStep.Children
            .Select(childId => FindById(childId))
            .Where(s => s != null)
            .Cast<WorkflowStep>()
            .ToList();
    }

    /// <summary>
    /// 获取指定步骤的所有后续步骤
    /// </summary>
    public List<WorkflowStep> GetNextSteps(int stepId)
    {
        var step = FindById(stepId);
        if (step == null)
        {
            return new List<WorkflowStep>();
        }

        return step.Outcomes
            .Where(o => o.NextStep > 0)
            .Select(o => FindById(o.NextStep))
            .Where(s => s != null)
            .Cast<WorkflowStep>()
            .ToList();
    }

    /// <summary>
    /// 检查步骤是否存在
    /// </summary>
    public bool Contains(int stepId)
    {
        return _dictionary.ContainsKey(stepId);
    }

    /// <summary>
    /// 移除步骤
    /// </summary>
    public bool RemoveById(int id)
    {
        return _dictionary.Remove(id);
    }
}
