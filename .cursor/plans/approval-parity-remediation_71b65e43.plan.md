---
name: approval-parity-remediation
overview: 对审批流实现做等保合规与质量性能整改，覆盖鉴权、审计、数据保护、回调安全、幂等与性能热点。
todos:
  - id: auth-audit
    content: 补齐鉴权校验与审计留痕（控制器/运行时服务）
    status: completed
  - id: callback-security
    content: 修复回调安全与敏感数据保护（URL白名单/SecretKey加密/日志）
    status: completed
  - id: idempotency-concurrency
    content: 增加幂等与并发控制，清理 Task.Run 吞异常
    status: completed
  - id: perf-index
    content: 性能优化与索引补齐（批量/去重复扫描/索引）
    status: completed
  - id: quality-gate
    content: 更新 Approval.http 并确保 build 0 warnings
    status: completed
isProject: false
---

## 关键问题清单与定位

- 鉴权/越权风险：审批通过/驳回未校验处理人；解析 UserId 失败时抛异常；管理员权限 TODO 未实现。[`d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalRuntimeCommandService.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalRuntimeCommandService.cs), [`d:/Code/SecurityPlatform/src/backend/Atlas.WebApi/Controllers/ApprovalRuntimeController.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.WebApi/Controllers/ApprovalRuntimeController.cs)
- 审计留痕缺失：关键操作未写 `IAuditWriter`；操作记录依赖可选幂等键导致审计不完整。[`d:/Code/SecurityPlatform/src/backend/Atlas.WebApi/Controllers/ApprovalTasksController.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.WebApi/Controllers/ApprovalTasksController.cs), [`d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalOperationService.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalOperationService.cs)
- 数据保护/外部回调：回调体直接包含 `DataJson`；回调 URL 无白名单校验；SecretKey 明文存储；回调异常未记录。[`d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalFlow/ExternalCallbackService.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalFlow/ExternalCallbackService.cs), [`d:/Code/SecurityPlatform/src/backend/Atlas.Domain.Approval/Entities/ApprovalExternalCallbackConfig.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.Domain.Approval/Entities/ApprovalExternalCallbackConfig.cs)
- 幂等与并发：审批/驳回无幂等保护；实例状态更新无并发控制；大量 `_ = Task.Run` 异常被吞掉。[`d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalRuntimeCommandService.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalRuntimeCommandService.cs), [`d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalFlow/FlowEngine.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalFlow/FlowEngine.cs)
- 性能问题：N+1 查询、批量写入缺失、重复扫描、索引不足。[`d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalFlow/ApprovalUserService.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalFlow/ApprovalUserService.cs), [`d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalFlow/Operations/AddAssigneeOperationHandler.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Services/ApprovalFlow/Operations/AddAssigneeOperationHandler.cs), [`d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Repositories/ApprovalTaskRepository.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Repositories/ApprovalTaskRepository.cs), [`d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Repositories/ApprovalParallelTokenRepository.cs`](d:/Code/SecurityPlatform/src/backend/Atlas.Infrastructure/Repositories/ApprovalParallelTokenRepository.cs)

## 修复方案

1. **鉴权与安全**：在运行时服务中校验审批人归属；统一获取 `UserId` 并做健壮性处理；补齐管理员权限校验。
2. **审计留痕**：关键入口（发起/审批/驳回/撤回等）写入 `IAuditWriter`；操作记录改为强制记录并关联幂等键。
3. **回调与数据保护**：回调体做字段白名单或脱敏；回调 URL 白名单校验；`SecretKey` 加密存储与使用时解密；补全回调异常日志与失败重试记录。
4. **幂等与并发控制**：审批/驳回引入幂等键或操作记录防重；实例状态更新引入乐观锁/版本检查；清理 `Task.Run` 吞异常，改为可追踪日志或统一后台队列。
5. **性能优化与索引**：将循环查询/写入改批量；避免重复扫描；补充高频查询的复合索引与字段索引。
6. **质量门禁**：更新 `Bosch.http/Approval.http` 用例覆盖关键操作；执行 `dotnet build` 确保 0 warnings。