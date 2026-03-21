$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\src\backend\Atlas.Infrastructure')).Path
$replacements = @(
    @{ Pattern = 'throw new BusinessException\("INSTANCE_NOT_FOUND", "流程实例不存在"\)'; Replacement = 'throw new BusinessException("INSTANCE_NOT_FOUND", "ApprovalInstanceNotFound")' }
    @{ Pattern = 'throw new Core\.Exceptions\.BusinessException\("INSTANCE_NOT_FOUND", "流程实例不存在"\)'; Replacement = 'throw new Core.Exceptions.BusinessException("INSTANCE_NOT_FOUND", "ApprovalInstanceNotFound")' }
    @{ Pattern = 'throw new BusinessException\("TASK_NOT_FOUND", "任务不存在"\)'; Replacement = 'throw new BusinessException("TASK_NOT_FOUND", "ApprovalTaskNotFound")' }
    @{ Pattern = 'throw new BusinessException\("TASK_NOT_FOUND", "审批任务不存在"\)'; Replacement = 'throw new BusinessException("TASK_NOT_FOUND", "ApprovalTaskNotFound")' }
    @{ Pattern = 'throw new BusinessException\("INSTANCE_NOT_RUNNING", "流程实例不在运行状态"\)'; Replacement = 'throw new BusinessException("INSTANCE_NOT_RUNNING", "ApprovalInstanceNotRunning")' }
    @{ Pattern = 'throw new BusinessException\("FLOW_NOT_FOUND", "流程定义不存在"\)'; Replacement = 'throw new BusinessException("FLOW_NOT_FOUND", "ApprovalFlowDefNotFoundShort")' }
    @{ Pattern = 'if \(flowDef == null\) throw new BusinessException\("FLOW_NOT_FOUND", "流程定义不存在"\)'; Replacement = 'if (flowDef == null) throw new BusinessException("FLOW_NOT_FOUND", "ApprovalFlowDefNotFoundShort")' }
    @{ Pattern = 'throw new BusinessException\("TASK_ID_REQUIRED", "加签操作需要指定任务ID"\)'; Replacement = 'throw new BusinessException("TASK_ID_REQUIRED", "ApprovalOpTaskIdRequired")' }
    @{ Pattern = 'throw new BusinessException\("ASSIGNEE_REQUIRED", "加签操作需要指定至少一个审批人"\)'; Replacement = 'throw new BusinessException("ASSIGNEE_REQUIRED", "ApprovalOpAssigneeRequired")' }
    @{ Pattern = 'throw new BusinessException\("TASK_INSTANCE_MISMATCH", "任务不属于指定的流程实例"\)'; Replacement = 'throw new BusinessException("TASK_INSTANCE_MISMATCH", "ApprovalOpTaskInstanceMismatch")' }
    @{ Pattern = 'throw new BusinessException\("TASK_NOT_PENDING", "只能对待审批的任务进行加签"\)'; Replacement = 'throw new BusinessException("TASK_NOT_PENDING", "ApprovalOpOnlyPendingTask")' }
    @{ Pattern = 'throw new BusinessException\("FORBIDDEN", "只有当前处理人可以执行加签操作"\)'; Replacement = 'throw new BusinessException("FORBIDDEN", "ApprovalOpOnlyCurrentHandler")' }
    @{ Pattern = 'throw new BusinessException\("TASK_ID_REQUIRED", "转办操作需要指定任务ID"\)'; Replacement = 'throw new BusinessException("TASK_ID_REQUIRED", "ApprovalOpTaskIdRequired")' }
    @{ Pattern = 'throw new BusinessException\("TARGET_ASSIGNEE_REQUIRED", "转办操作需要指定目标处理人"\)'; Replacement = 'throw new BusinessException("TARGET_ASSIGNEE_REQUIRED", "ApprovalOpTargetAssigneeRequired")' }
    @{ Pattern = 'throw new BusinessException\("TASK_NOT_PENDING", "只能转办待审批的任务"\)'; Replacement = 'throw new BusinessException("TASK_NOT_PENDING", "ApprovalOpOnlyPendingTask")' }
    @{ Pattern = 'throw new BusinessException\("UNAUTHORIZED", "只能转办自己的任务"\)'; Replacement = 'throw new BusinessException("UNAUTHORIZED", "ApprovalOpOnlyOwnTask")' }
    @{ Pattern = 'throw new BusinessException\("NO_CURRENT_NODE", "流程实例没有当前节点"\)'; Replacement = 'throw new BusinessException("NO_CURRENT_NODE", "ApprovalOpNoCurrentNode")' }
    @{ Pattern = 'throw new BusinessException\("TARGET_NODE_REQUIRED", "未来节点加签操作需要指定目标节点ID"\)'; Replacement = 'throw new BusinessException("TARGET_NODE_REQUIRED", "ApprovalOpTargetNodeRequired")' }
    @{ Pattern = 'throw new BusinessException\("ASSIGNEE_REQUIRED", "未来节点加签操作需要指定至少一个审批人"\)'; Replacement = 'throw new BusinessException("ASSIGNEE_REQUIRED", "ApprovalOpAssigneeRequired")' }
    @{ Pattern = 'throw new BusinessException\("NODE_NOT_FOUND", "目标节点不存在"\)'; Replacement = 'throw new BusinessException("NODE_NOT_FOUND", "ApprovalOpTargetNodeNotFound")' }
    @{ Pattern = 'throw new BusinessException\("NODE_ALREADY_EXECUTED", "目标节点已经执行，无法进行未来节点加签"\)'; Replacement = 'throw new BusinessException("NODE_ALREADY_EXECUTED", "ApprovalOpNodeAlreadyExecuted")' }
    @{ Pattern = 'throw new BusinessException\("TASK_ID_REQUIRED", "撤销同意操作需要指定任务ID"\)'; Replacement = 'throw new BusinessException("TASK_ID_REQUIRED", "ApprovalOpTaskIdRequired")' }
    @{ Pattern = 'throw new BusinessException\("TASK_NOT_APPROVED", "只能撤销已同意的任务"\)'; Replacement = 'throw new BusinessException("TASK_NOT_APPROVED", "ApprovalOpTaskNotApproved")' }
    @{ Pattern = 'throw new BusinessException\("UNAUTHORIZED", "只能撤销自己的审批"\)'; Replacement = 'throw new BusinessException("UNAUTHORIZED", "ApprovalOpOnlyOwnTask")' }
    @{ Pattern = 'throw new BusinessException\("TARGET_NODE_REQUIRED", "变更未来节点处理人操作需要指定目标节点ID"\)'; Replacement = 'throw new BusinessException("TARGET_NODE_REQUIRED", "ApprovalOpTargetNodeRequired")' }
    @{ Pattern = 'throw new BusinessException\("TARGET_ASSIGNEE_REQUIRED", "变更未来节点处理人操作需要指定目标处理人"\)'; Replacement = 'throw new BusinessException("TARGET_ASSIGNEE_REQUIRED", "ApprovalOpTargetAssigneeRequired")' }
    @{ Pattern = 'throw new BusinessException\("NODE_ALREADY_EXECUTED", "目标节点已经执行，无法变更未来节点处理人"\)'; Replacement = 'throw new BusinessException("NODE_ALREADY_EXECUTED", "ApprovalOpNodeAlreadyExecuted")' }
    @{ Pattern = 'throw new BusinessException\("TASK_ID_REQUIRED", "打回修改操作需要指定任务ID"\)'; Replacement = 'throw new BusinessException("TASK_ID_REQUIRED", "ApprovalOpTaskIdRequired")' }
    @{ Pattern = 'throw new BusinessException\("TASK_NOT_PENDING", "只能打回待审批的任务"\)'; Replacement = 'throw new BusinessException("TASK_NOT_PENDING", "ApprovalOpOnlyPendingTask")' }
    @{ Pattern = 'throw new BusinessException\("FORBIDDEN", "只有当前处理人可以执行打回修改操作"\)'; Replacement = 'throw new BusinessException("FORBIDDEN", "ApprovalOpOnlyCurrentHandler")' }
    @{ Pattern = 'throw new BusinessException\("TARGET_ASSIGNEE_REQUIRED", "加批操作需要指定审批人"\)'; Replacement = 'throw new BusinessException("TARGET_ASSIGNEE_REQUIRED", "ApprovalOpAssigneeRequired")' }
    @{ Pattern = 'throw new BusinessException\("TASK_ID_REQUIRED", "转发操作需要指定任务ID"\)'; Replacement = 'throw new BusinessException("TASK_ID_REQUIRED", "ApprovalOpTaskIdRequired")' }
    @{ Pattern = 'throw new BusinessException\("TARGET_ASSIGNEE_REQUIRED", "转发操作需要指定目标用户"\)'; Replacement = 'throw new BusinessException("TARGET_ASSIGNEE_REQUIRED", "ApprovalOpTargetAssigneeRequired")' }
    @{ Pattern = 'throw new BusinessException\("UNAUTHORIZED", "只能转发自己的任务"\)'; Replacement = 'throw new BusinessException("UNAUTHORIZED", "ApprovalOpOnlyOwnTask")' }
    @{ Pattern = 'throw new BusinessException\("INVALID_REQUEST", "跳转目标节点不能为空"\)'; Replacement = 'throw new BusinessException("INVALID_REQUEST", "ApprovalOpJumpTargetRequired")' }
    @{ Pattern = 'throw new BusinessException\("TASK_ID_REQUIRED", "承办操作需要指定任务ID"\)'; Replacement = 'throw new BusinessException("TASK_ID_REQUIRED", "ApprovalOpTaskIdRequired")' }
)

Get-ChildItem -Path $root -Recurse -Filter *.cs | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName)
    $original = $content
    foreach ($item in $replacements) {
        $content = $content -replace $item.Pattern, $item.Replacement
    }
    if ($content -ne $original) {
        [System.IO.File]::WriteAllText($_.FullName, $content)
        Write-Host "Updated: $($_.FullName)"
    }
}
Write-Host 'Done'
