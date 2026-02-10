---
name: FlowLong Gap Analysis
overview: After deep comparison between the FlowLong Java competitor code and the SecurityPlatform C# implementation, 20 gaps/inconsistencies have been identified across engine logic, enums, entities, and configuration parsing.
todos:
  - id: fix-reject-routing
    content: "P0: 补全驳回路由逻辑 - RejectStrategy 扩展(+2值) + FlowEngine.HandleRejectionAsync() + 分支内驳回特殊处理"
    status: completed
  - id: fix-reapprove-strategy
    content: "P0: 接入 ReApproveStrategy - 在 AdvanceFlowAsync 中处理 rejectJump 任务类型时根据 ReApproveStrategy 决定继续/退回"
    status: completed
  - id: add-node-approve-self
    content: "P0: 新增 NodeApproveSelf 枚举 + FlowNode.ApproveSelf 属性 + ParseNode 解析 + GenerateTasksForNodeAsync 判断逻辑"
    status: completed
  - id: impl-subprocess
    content: "P0: 完善子流程 - HandleSubProcessAsync 创建子流程实例 + EndSubProcessAsync 回调主流程 + SubProcessLink 记录"
    status: completed
  - id: impl-timer-trigger
    content: "P1: 完善定时器/触发器节点 - 创建 TimerJob/TriggerJob 记录 + Job 到期自动推进 + 触发器执行接口"
    status: completed
  - id: fix-timeout-parsing
    content: "P1: 修复超时配置解析 - ParseNode 中补充 TimeoutEnabled/TimeoutHours/TimeoutMinutes/TimeoutAction 字段解析"
    status: completed
  - id: impl-agent-runtime
    content: "P1: 接入代理人运行时逻辑 - GenerateTasksForNodeAsync 中查询 AgentConfig 并替换审批人"
    status: completed
  - id: add-flow-data-transfer
    content: "P1: 新增 FlowExecutionContext (AsyncLocal) 支持运行时动态审批人分配和条件节点指定"
    status: completed
  - id: fix-branch-reject
    content: "P1: 补充并行/包容分支内驳回时强制终止兄弟分支活跃任务的逻辑"
    status: completed
  - id: misc-p2-items
    content: "P2: 模型节点加签/TaskCreateInterceptor/CC分支处理/顺序审批边缘/父节点关系 (按需实施)"
    status: in_progress
isProject: false
---

# FlowLong 竞品对比差距审查报告

对比 FlowLong (`C:\Users\kuo13\Downloads\flowlong`) 与 SecurityPlatform 当前实现，以下是尚未落实或与竞品不一致的关键差距，按严重程度排序。

---

## 一、驳回路由逻辑完全未接入 [严重]

**FlowLong 实现**: `FlowLongEngineImpl.executeRejectTask()` 根据 `nodeModel.getRejectStrategy()` 自动路由：

- `TO_INITIATOR(1)` -- 跳转到发起人节点
- `TO_PREVIOUS_NODE(2)` -- 退回上一节点（undoHisTask 恢复历史任务）
- `TO_SPECIFIED_NODE(3)` -- 跳转到指定节点
- `TERMINATE_APPROVAL(4)` -- 终止流程
- `TO_PARENT_NODE(5)` -- 跳转到模型父节点

**SecurityPlatform 现状**:

- `RejectStrategy` 枚举只有 3 个值（缺少 `TerminateApproval=4` 和 `ToParentNode=5`）
- `FlowNode.RejectStrategy` 属性存在但 **从未被使用**
- `RejectTaskAsync()` 在 `ApprovalRuntimeCommandService` 中直接将实例标记为 Rejected 并取消所有任务，**没有任何路由逻辑**

**需要补充**:

1. `RejectStrategy` 枚举增加 `TerminateApproval = 4` 和 `ToParentNode = 5`
2. 在 `FlowEngine` 中新增 `HandleRejectionAsync()` 方法，根据节点的 `RejectStrategy` 执行不同驳回路由
3. 支持驳回后跳转到指定节点（复用 `JumpToNodeAsync`）
4. 支持并行/包容分支内驳回时强制结束兄弟分支任务

---

## 二、重新审批策略 (ReApproveStrategy) 未接入 [严重]

**FlowLong 实现**: `afterDoneTask()` 中处理 `TaskType.rejectJump` 类型任务时，根据 `parentNodeModel.getRejectStart()` 决定：

- `1` = 继续往后执行
- `2` = 跳转回驳回源节点

**SecurityPlatform 现状**: `ReApproveStrategy` 枚举和 `FlowNode.ReApproveStrategy` 属性存在但**从未被引擎使用**。驳回后重新提交时无法区分"继续执行"与"退回到驳回节点"。

---

## 三、审批人与提交人相同处理 (NodeApproveSelf) 完全缺失 [严重]

**FlowLong 实现**: `NodeApproveSelf` 枚举 + `NodeModel.approveSelf` 属性：

- `0` = 由发起人对自己审批（默认）
- `1` = 自动跳过
- `2` = 转交给直接上级审批
- `3` = 转交给部门负责人审批

在 `TaskServiceImpl.createTask()` 中，创建任务时检查审批人是否与提交人相同，根据策略自动处理。

**SecurityPlatform 现状**:

- 无 `NodeApproveSelf` 枚举
- `FlowNode` 无 `ApproveSelf` 属性
- `FlowEngine` 无相关逻辑
- JSON 不解析此字段

**需要补充**:

1. 新建 `NodeApproveSelf` 枚举
2. `FlowNode` 增加 `ApproveSelf` 属性
3. `ParseNode()` 解析 `approveSelf` 字段
4. `GenerateTasksForNodeAsync()` 中增加判断逻辑

---

## 四、子流程实现为空壳 [严重]

**FlowLong 实现**:

- `Execution.createSubExecution()` 创建子执行上下文
- `RuntimeServiceImpl.createInstance()` 支持设置 `parentInstanceId`
- `Execution.endInstance()` 中检查 `existActiveSubProcess()` 防止过早结束
- 子流程结束后通过 `FlowLongEngineImpl` 回调主流程

**SecurityPlatform 现状**:

- `HandleSubProcessAsync()` 只有注释，有 bug（传入 `null!` 作为 FlowDefinition）
- `EndSubProcessAsync()` 方法体为空
- `ApprovalSubProcessLink` 实体存在但未使用

---

## 五、定时器/触发器节点为占位实现 [中等]

**FlowLong 实现**:

- `FlowLongScheduler.remind()` 定时扫描超时/提醒任务
- 定时器/触发器任务到期后调用 `autoCompleteTask()` 自动完成
- `NodeModel.executeTrigger()` 支持动态加载触发器类执行
- `termAuto` + `termMode` 控制超时自动审批/拒绝

**SecurityPlatform 现状**:

- Job 类 (`ApprovalTimerNodeJob`, `ApprovalTriggerNodeJob`) 存在但引擎中为占位
- 当前 Timer/Trigger 节点直接自动通过并继续推进
- 未创建 `ApprovalTimerJob` / `ApprovalTriggerJob` 记录
- `FlowNode.TimerConfig` 和 `FlowNode.TriggerType` 属性存在但未使用

---

## 六、超时配置未从 JSON 解析 [中等]

**FlowLong 实现**: `NodeModel` 有 `termAuto`(Boolean), `term`(Integer 小时), `termMode`(0=自动通过/1=自动拒绝) 字段。

**SecurityPlatform 现状**:

- `FlowNode` 有 `TimeoutEnabled`, `TimeoutHours`, `TimeoutMinutes`, `TimeoutAction` 属性
- 但 `ParseNode()` 方法中**未解析这些字段**（搜索 FlowDefinitionParser.cs 的 ParseNode 方法，timeout 相关属性没有赋值逻辑）
- 设计器可能配置了超时但后端不会读取

---

## 七、代理人运行时逻辑缺失 [中等]

**FlowLong 实现**:

- `FlwTaskActor` 有 `agentId` + `agentType` 字段
- 创建任务时检查 `ApprovalAgentConfig` 是否有生效的代理配置
- 代理人审批后，原任务标记完成
- `TaskType.agent(13)` 专门用于代理任务

**SecurityPlatform 现状**:

- `ApprovalAgentConfig` 实体存在
- `ApprovalAgentController` 存在（CRUD API）
- 但 `FlowEngine.GenerateTasksForNodeAsync()` 中**无代理人替换逻辑**
- 任务创建时不检查代理配置

---

## 八、流程数据传输机制 (FlowDataTransfer) 缺失 [中等]

**FlowLong 实现**: `FlowDataTransfer`（ThreadLocal）支持：

- 运行时动态分配审批人 (`dynamicAssignee`)
- 指定条件节点选择 (`specifyConditionNodeKey`)
- 在包容分支中标记最后一个条件节点 (`processLastConditionNode`)

**SecurityPlatform 现状**: 无等效的 `AsyncLocal<T>` 或上下文传递机制。运行时决策数据无法在节点间传递。

---

## 九、并行/包容分支内驳回特殊处理缺失 [中等]

**FlowLong 实现**: `TaskServiceImpl.rejectTask()` 中：

- 检查当前节点是否在并行/包容分支内
- 如果是，强制终止所有兄弟分支的活跃任务
- 然后执行驳回逻辑

**SecurityPlatform 现状**: 驳回逻辑直接终止整个实例，未考虑分支内驳回场景。

---

## 十、动态追加/移除模型节点 (加签/减签模型操作) 差异 [低]

**FlowLong 实现**: `RuntimeServiceImpl.appendNodeModel()` / `removeNodeModel()` **修改运行时流程模型**，在指定任务前后插入新审批节点。

**SecurityPlatform 现状**: 有 `AddAssigneeOperationHandler` / `RemoveAssigneeOperationHandler`，但操作的是**审批人**而非模型节点。当前加签只增加审批人，不会在流程图中插入新节点。

---

## 十一、TaskCreateInterceptor (任务创建拦截器) 缺失 [低]

**FlowLong 实现**: `TaskCreateInterceptor` 接口提供 `before()` / `after()` 钩子，允许在任务创建前后执行自定义逻辑。

**SecurityPlatform 现状**: 无等效拦截器。`ApprovalEventPublisher` 只发布事件，不能拦截/修改任务创建。

---

## 十二、CC 节点在分支中的特殊处理缺失 [低]

**FlowLong 实现**: `NodeModel.ccExecNextNode()` 对 CC 节点在并行/包容分支中的行为有特殊处理（只有最后一个并行分支的 CC 节点才推进后续节点）。

**SecurityPlatform 现状**: CC 节点在所有场景下都直接推进到下一个节点，可能导致并行分支中 CC 节点过早触发后续合并节点。

---

## 十三、顺序审批 (Sequential) 边缘场景缺失 [低]

**FlowLong 实现**: `afterDoneTask()` 中 `PerformType.sort` 处理：

- 区分主管、角色、部门的当前审批人确认方式
- 支持转办后继续顺序审批（使用 `assignorId`）
- 支持分组策略全员参与的顺序审批

**SecurityPlatform 现状**: `ActivateNextSequentialTaskAsync()` 按 `Order` 简单激活下一个等待任务，未处理主管/角色/部门特殊逻辑。

---

## 十四、流程模型父节点关系构建缺失 [低]

**FlowLong 实现**: `ProcessModel.buildParentNode()` 在解析时构建完整的父子关系，`NodeModel.parentApprovalNode()` 可递归查找父审批节点。

**SecurityPlatform 现状**: 使用图结构（edges），可以通过 `GetIncomingEdges()` 反查，但没有预构建的父节点关系，也没有 `FindParentApprovalNode()` 方法。

---

## 实施优先级建议

| 优先级 | 差距项 | 影响范围 |
|--------|--------|----------|
| P0 | 驳回路由逻辑 | 审批核心功能 |
| P0 | 重新审批策略 | 审批核心功能 |
| P0 | NodeApproveSelf | 审批核心功能 |
| P0 | 子流程实现 | 高级流程 |
| P1 | 定时器/触发器 | 高级流程 |
| P1 | 超时配置解析 | 超时自动处理 |
| P1 | 代理人运行时 | 代理审批 |
| P1 | FlowDataTransfer | 运行时决策 |
| P1 | 分支内驳回 | 并行/包容分支 |
| P2 | 模型节点加签 | 高级操作 |
| P2 | 任务创建拦截器 | 扩展性 |
| P2 | CC 分支处理 | 并行分支 |
| P2 | 顺序审批边缘 | 顺序审批 |
| P2 | 父节点关系 | 引擎内部 |