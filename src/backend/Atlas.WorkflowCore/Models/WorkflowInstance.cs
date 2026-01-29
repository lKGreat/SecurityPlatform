using System.Collections;
using Atlas.WorkflowCore.Abstractions;

namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 工作流实例
/// </summary>
public class WorkflowInstance : ISearchable
{
    public string Id { get; set; } = string.Empty;

    public string WorkflowDefinitionId { get; set; } = string.Empty;

    public int Version { get; set; }

    public string? Description { get; set; }

    public string? Reference { get; set; }

    public ExecutionPointerCollection ExecutionPointers { get; set; } = new();

    public long? NextExecution { get; set; }

    public WorkflowStatus Status { get; set; }

    public object? Data { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime? CompleteTime { get; set; }

    public List<ExecutionError> ExecutionErrors { get; set; } = new();

    public bool IsBranchComplete(string parentId)
    {
        return ExecutionPointers
            .FindByScope(parentId)
            .All(x => x.EndTime != null);
    }

    /// <summary>
    /// 获取搜索令牌（用于全文搜索）
    /// </summary>
    public IEnumerable<string> GetSearchTokens()
    {
        var tokens = new List<string>
        {
            Id,
            WorkflowDefinitionId,
            Version.ToString(),
            Status.ToString()
        };

        if (!string.IsNullOrEmpty(Reference))
        {
            tokens.Add(Reference);
        }

        // 添加执行指针相关的搜索令牌
        foreach (var pointer in ExecutionPointers)
        {
            if (!string.IsNullOrEmpty(pointer.StepName))
            {
                tokens.Add(pointer.StepName);
            }
        }

        return tokens;
    }
}

public class ExecutionPointerCollection : ICollection<ExecutionPointer>
{
    private readonly Dictionary<string, ExecutionPointer> _dictionary = new Dictionary<string, ExecutionPointer>();
    private readonly Dictionary<string, ICollection<ExecutionPointer>> _scopeMap = new Dictionary<string, ICollection<ExecutionPointer>>();

    public ExecutionPointerCollection()
    {
    }

    public ExecutionPointerCollection(int capacity)
    {
        _dictionary = new Dictionary<string, ExecutionPointer>(capacity);
    }

    public ExecutionPointerCollection(ICollection<ExecutionPointer> pointers)
    {
        foreach (var ptr in pointers)
        {
            Add(ptr);
        }
    }

    public IEnumerator<ExecutionPointer> GetEnumerator()
    {
        return _dictionary.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public ExecutionPointer? FindById(string id)
    {
        if (!_dictionary.ContainsKey(id))
            return null;

        return _dictionary[id];
    }

    public ICollection<ExecutionPointer> FindByScope(string stackFrame)
    {
        if (!_scopeMap.ContainsKey(stackFrame))
            return new List<ExecutionPointer>();

        return _scopeMap[stackFrame];
    }

    public void Add(ExecutionPointer item)
    {
        _dictionary.Add(item.Id, item);

        foreach (var stackFrame in item.Scope)
        {
            if (!_scopeMap.ContainsKey(stackFrame))
                _scopeMap.Add(stackFrame, new List<ExecutionPointer>());
            _scopeMap[stackFrame].Add(item);
        }
    }

    public void Clear()
    {
        _dictionary.Clear();
        _scopeMap.Clear();
    }

    public bool Contains(ExecutionPointer item)
    {
        return _dictionary.ContainsValue(item);
    }

    public void CopyTo(ExecutionPointer[] array, int arrayIndex)
    {
        _dictionary.Values.CopyTo(array, arrayIndex);
    }

    public bool Remove(ExecutionPointer item)
    {
        foreach (var stackFrame in item.Scope)
        {
            if (_scopeMap.ContainsKey(stackFrame))
                _scopeMap[stackFrame].Remove(item);
        }

        return _dictionary.Remove(item.Id);
    }

    public ExecutionPointer? Find(Predicate<ExecutionPointer> match)
    {
        return _dictionary.Values.FirstOrDefault(x => match(x));
    }

    public ICollection<ExecutionPointer> FindByStatus(PointerStatus status)
    {
        //TODO: track states in hash table for O(1)
        return _dictionary.Values.Where(x => x.Status == status).ToList();
    }

    public int Count => _dictionary.Count;
    public bool IsReadOnly => false;

    public IEnumerable<ExecutionPointer> FindActive()
    {
        return _dictionary.Values.Where(x => x.Active && x.EndTime == null);
    }

    /// <summary>
    /// 根据步骤ID查找所有执行指针
    /// </summary>
    public List<ExecutionPointer> FindByStepId(int stepId)
    {
        return _dictionary.Values.Where(p => p.StepId == stepId).ToList();
    }

    /// <summary>
    /// 获取所有活动的执行指针（状态为Running或Pending）
    /// </summary>
    public List<ExecutionPointer> GetActivePointers()
    {
        return _dictionary.Values.Where(p => 
            p.Status == PointerStatus.Running || 
            p.Status == PointerStatus.Pending).ToList();
    }

    /// <summary>
    /// 获取所有等待的执行指针（状态为WaitingForEvent）
    /// </summary>
    public List<ExecutionPointer> GetWaitingPointers()
    {
        return _dictionary.Values.Where(p => p.Status == PointerStatus.WaitingForEvent).ToList();
    }

    /// <summary>
    /// 获取所有已完成的执行指针
    /// </summary>
    public List<ExecutionPointer> GetCompletedPointers()
    {
        return _dictionary.Values.Where(p => p.Status == PointerStatus.Complete).ToList();
    }

    /// <summary>
    /// 获取所有失败的执行指针
    /// </summary>
    public List<ExecutionPointer> GetFailedPointers()
    {
        return _dictionary.Values.Where(p => p.Status == PointerStatus.Failed).ToList();
    }

    /// <summary>
    /// 检查是否存在指定状态的指针
    /// </summary>
    public bool HasPointersWithStatus(PointerStatus status)
    {
        return _dictionary.Values.Any(p => p.Status == status);
    }
}
