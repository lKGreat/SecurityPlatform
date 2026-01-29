---
name: WorkflowCore完整功能对比与补齐计划
overview: 对比开源workflow-core-master和当前Atlas.WorkflowCore项目，识别简化实现和缺失功能，创建完整的补齐任务列表，确保实现颗粒度对齐。
todos:
  - id: workflow-host-complete
    content: WorkflowHost完整实现：添加公开依赖属性、Start()同步方法、AddEventSubscriptions方法、统一Activity API接口
    status: completed
  - id: workflow-executor-middleware
    content: WorkflowExecutor中间件集成：InitializeStep中添加StepStarted事件发布、Execute中添加RunExecuteMiddleware调用、DetermineNextExecutionTime中添加RunPostMiddleware调用
    status: completed
  - id: event-consumer-complete
    content: EventConsumer完整实现：添加ActivityResult处理、实现SeedSubscription方法、添加事件顺序处理逻辑、完善事件订阅匹配
    status: completed
  - id: queue-consumer-concurrency
    content: QueueConsumer并发控制：添加MaxConcurrentItems、实现SecondPasses支持、添加活动任务跟踪、实现EventWaitHandle同步机制
    status: completed
  - id: runnable-poller-commands
    content: RunnablePoller完整实现：添加PollCommands方法、完善ScheduledCommand支持、确保PollWorkflows和PollEvents使用ScheduledCommand
    status: completed
  - id: workflow-consumer-scheduled
    content: WorkflowConsumer完善：完善ScheduledCommand支持、完善FutureQueue实现、完善TryProcessSubscription逻辑
    status: completed
  - id: index-consumer-pool
    content: IndexConsumer完整实现：添加对象池支持、实现错误重试机制、添加EnableSecondPasses支持、添加错误计数管理
    status: completed
  - id: middleware-runner-chain
    content: WorkflowMiddlewareRunner链式调用：实现Reverse+Aggregate模式、添加WorkflowDefinition错误处理类型支持、实现错误处理中间件查找
    status: completed
  - id: step-collection-optimize
    content: WorkflowStepCollection优化：重构为Dictionary内部存储、实现O(1)的FindById查找、保持现有辅助方法
    status: completed
  - id: pointer-collection-optimize
    content: ExecutionPointerCollection优化：重构为Dictionary内部存储、添加范围映射、实现O(1)查找、添加FindByStatus方法
    status: completed
  - id: default-providers
    content: 默认提供者实现：实现MemoryPersistenceProvider和TransientMemoryPersistenceProvider、确保线程安全
    status: completed
  - id: injected-pool-policy
    content: 工具类实现：实现InjectedObjectPoolPolicy<T>、确保WorkflowActivityTracing功能对齐
    status: completed
isProject: false
---

# WorkflowCore完整功能对比与补齐计划

## 一、能力清单对比

### 1. 核心服务能力

#### 开源版本 (workflow-core-master)

1. **WorkflowHost** - 工作流主机

- 公开依赖属性（PersistenceStore, LockProvider, Registry, Options, QueueProvider, Logger）
- Start()同步方法和StartAsync()异步方法
- Stop()同步方法和StopAsync()异步方法
- 生命周期事件订阅管理（AddEventSubscriptions）
- Activity API（GetPendingActivity, ReleaseActivityToken, SubmitActivitySuccess/Failure）
- 事件委托（OnStepError, OnLifeCycleEvent）

2. **WorkflowExecutor** - 工作流执行器

- 完整的步骤初始化（InitializeStep）包含StepStarted事件发布
- 执行后中间件调用（RunExecuteMiddleware）
- 完成后中间件调用（RunPostMiddleware）
- 完整的DetermineNextExecutionTime逻辑（包含中间件调用和事件发布）

3. **WorkflowController** - 工作流控制器

- 多个StartWorkflow重载（支持泛型和版本）
- PreWorkflow中间件调用
- WorkflowStarted事件发布
- 完整的锁管理

4. **WorkflowConsumer** - 工作流消费者

- 完整的错误处理
- FutureQueue延迟队列支持
- ScheduledCommand支持
- 完整的事件订阅处理（TryProcessSubscription）
- 灰名单管理

5. **EventConsumer** - 事件消费者

- ActivityResult特殊处理
- SeedSubscription方法（处理事件顺序）
- 完整的事件订阅匹配逻辑
- 工作流唤醒逻辑

6. **IndexConsumer** - 索引消费者

- 对象池支持（InjectedObjectPoolPolicy）
- 错误重试机制（最多20次，带延迟）
- EnableSecondPasses支持
- 错误计数管理

7. **RunnablePoller** - 可运行实例轮询器

- PollWorkflows（轮询可运行工作流）
- PollEvents（轮询未处理事件）
- PollCommands（轮询计划命令）
- ScheduledCommand支持

8. **QueueConsumer** - 队列消费者基类

- 并发控制（MaxConcurrentItems）
- SecondPasses支持
- 活动任务跟踪（_activeTasks字典）
- EventWaitHandle同步机制

9. **WorkflowMiddlewareRunner** - 中间件运行器

- 链式中间件调用（Reverse + Aggregate）
- WorkflowDefinition错误处理类型支持
- 错误处理中间件（IWorkflowMiddlewareErrorHandler）

10. **LifeCycleEventHub** - 生命周期事件中心

- SingleNodeEventHub实现
- Action<LifeCycleEvent>订阅模式
- 简单的发布-订阅机制

#### 当前项目 (Atlas.WorkflowCore)

1. **WorkflowHost** - 部分实现

- ❌ 缺少公开依赖属性
- ❌ 缺少Start()同步方法
- ❌ 缺少AddEventSubscriptions方法
- ✅ 有Activity API但接口不同

2. **WorkflowExecutor** - 简化实现

- ❌ InitializeStep中缺少StepStarted事件发布
- ❌ 缺少RunExecuteMiddleware调用
- ❌ 缺少RunPostMiddleware调用
- ❌ DetermineNextExecutionTime缺少中间件调用

3. **WorkflowController** - 简化实现

- ✅ 有基本功能
- ❌ 缺少PreWorkflow中间件调用（已实现但需要确认）
- ✅ 有事件发布

4. **WorkflowConsumer** - 简化实现

- ✅ 基本功能完整
- ❌ 缺少ScheduledCommand完整支持（部分实现）
- ✅ 有FutureQueue但实现不同

5. **EventConsumer** - 大幅简化

- ❌ 缺少ActivityResult处理
- ❌ 缺少SeedSubscription方法
- ❌ 缺少事件顺序处理逻辑

6. **IndexConsumer** - 大幅简化

- ❌ 缺少对象池支持
- ❌ 缺少错误重试机制
- ❌ 缺少EnableSecondPasses

7. **RunnablePoller** - 简化实现

- ✅ 有PollWorkflows和PollEvents
- ❌ 缺少PollCommands
- ❌ 缺少ScheduledCommand支持

8. **QueueConsumer** - 大幅简化

- ❌ 缺少并发控制
- ❌ 缺少SecondPasses支持
- ❌ 缺少活动任务跟踪

9. **WorkflowMiddlewareRunner** - 简化实现

- ❌ 缺少链式调用（使用简单循环）
- ❌ 缺少WorkflowDefinition错误处理类型支持

10. **LifeCycleEventHub** - 不同实现

- ✅ 有泛型订阅支持
- ⚠️ 实现方式不同但功能完整

### 2. 模型类能力

#### 开源版本

1. **WorkflowStepCollection** - 专门的集合类

- Dictionary<int, WorkflowStep>内部存储
- FindById O(1)查找
- Find方法支持

2. **ExecutionPointerCollection** - 专门的集合类

- Dictionary<string, ExecutionPointer>内部存储
- Dictionary<string, ICollection<ExecutionPointer>>范围映射
- FindById O(1)查找
- FindByScope O(1)查找
- FindByStatus方法

#### 当前项目

1. **WorkflowStepCollection** - 继承List

- ✅ 有FindById但O(n)查找
- ✅ 有额外辅助方法

2. **ExecutionPointerCollection** - 继承List

- ✅ 有FindByScope但O(n)查找
- ✅ 有额外辅助方法

### 3. 默认提供者能力

#### 开源版本

1. **MemoryPersistenceProvider** - 内存持久化提供者

- 完整的IPersistenceProvider实现
- 线程安全的集合操作

2. **TransientMemoryPersistenceProvider** - 瞬态内存提供者

- 包装ISingletonMemoryProvider

3. **SingleNodeEventHub** - 单节点事件中心

- 简单的内存事件中心

4. **SingleNodeLockProvider** - 单节点锁提供者

- 内存锁实现

5. **SingleNodeQueueProvider** - 单节点队列提供者

- 内存队列实现

6. **NullSearchIndex** - 空搜索索引

- 空实现

#### 当前项目

1. ❌ 缺少MemoryPersistenceProvider
2. ❌ 缺少TransientMemoryPersistenceProvider
3. ✅ 有LifeCycleEventHub（不同实现）
4. ✅ 有SingleNodeLockProvider
5. ✅ 有SingleNodeQueueProvider
6. ✅ 有NullSearchIndex

### 4. 工具类能力

#### 开源版本

1. **InjectedObjectPoolPolicy** - 注入对象池策略

- 支持对象池模式

2. **WorkflowActivity** - OpenTelemetry追踪

- Enrich方法
- StartHost, StartPoll, StartConsume方法

#### 当前项目

1. ❌ 缺少InjectedObjectPoolPolicy
2. ✅ 有WorkflowActivityTracing（不同实现）

## 二、待办任务清单

### 任务组1：WorkflowHost完整实现

- [ ] 添加公开依赖属性（PersistenceStore, LockProvider等）
- [ ] 添加Start()同步方法
- [ ] 添加AddEventSubscriptions方法
- [ ] 统一Activity API接口（GetPendingActivity返回PendingActivity类型）

### 任务组2：WorkflowExecutor完整实现

- [ ] InitializeStep中添加StepStarted事件发布
- [ ] Execute方法中添加RunExecuteMiddleware调用
- [ ] DetermineNextExecutionTime中添加RunPostMiddleware调用
- [ ] 确保完整的中间件集成

### 任务组3：WorkflowController完整实现

- [ ] 确认并完善PreWorkflow中间件调用
- [ ] 确保所有StartWorkflow重载都正确调用中间件

### 任务组4：WorkflowConsumer完整实现

- [ ] 完善ScheduledCommand支持（检查SupportsScheduledCommands）
- [ ] 完善FutureQueue实现（确保与开源版本一致）
- [ ] 完善TryProcessSubscription逻辑（包括事件顺序处理）

### 任务组5：EventConsumer完整实现

- [ ] 添加ActivityResult特殊处理逻辑
- [ ] 实现SeedSubscription方法
- [ ] 添加事件顺序处理逻辑（处理早于当前事件的事件）
- [ ] 完善事件订阅匹配（支持ExecutionPointerId和EventName+EventKey两种方式）

### 任务组6：IndexConsumer完整实现

- [ ] 添加对象池支持（使用InjectedObjectPoolPolicy）
- [ ] 实现错误重试机制（最多20次，带延迟）
- [ ] 添加EnableSecondPasses支持
- [ ] 添加错误计数管理

### 任务组7：RunnablePoller完整实现

- [ ] 添加PollCommands方法
- [ ] 完善ScheduledCommand支持
- [ ] 确保PollWorkflows和PollEvents使用ScheduledCommand（如果支持）

### 任务组8：QueueConsumer完整实现

- [ ] 添加并发控制（MaxConcurrentItems属性）
- [ ] 实现SecondPasses支持
- [ ] 添加活动任务跟踪（_activeTasks字典）
- [ ] 实现EventWaitHandle同步机制
- [ ] 添加EnableSecondPasses虚拟属性

### 任务组9：WorkflowMiddlewareRunner完整实现

- [ ] 实现链式中间件调用（Reverse + Aggregate模式）
- [ ] 添加WorkflowDefinition错误处理类型支持
- [ ] 实现错误处理中间件查找逻辑

### 任务组10：WorkflowStepCollection优化

- [ ] 重构为Dictionary<int, WorkflowStep>内部存储
- [ ] 实现O(1)的FindById查找
- [ ] 保持现有辅助方法

### 任务组11：ExecutionPointerCollection优化

- [ ] 重构为Dictionary<string, ExecutionPointer>内部存储
- [ ] 添加Dictionary<string, ICollection<ExecutionPointer>>范围映射
- [ ] 实现O(1)的FindById和FindByScope查找
- [ ] 添加FindByStatus方法
- [ ] 保持现有辅助方法

### 任务组12：默认提供者实现

- [ ] 实现MemoryPersistenceProvider（ISingletonMemoryProvider接口）
- [ ] 实现TransientMemoryPersistenceProvider
- [ ] 确保线程安全

### 任务组13：工具类实现

- [ ] 实现InjectedObjectPoolPolicy<T>
- [ ] 确保WorkflowActivityTracing与开源版本功能对齐

### 任务组14：模型完整性

- [ ] 检查WorkflowInstance是否缺少ExecutionErrors属性（当前项目有）
- [ ] 确保所有模型属性与开源版本对齐
- [ ] 检查WorkflowDefinition的OnPostMiddlewareError和OnExecuteMiddlewareError支持

## 三、实现优先级

### 高优先级（核心功能）

1. WorkflowExecutor的中间件集成
2. EventConsumer的完整事件处理逻辑
3. QueueConsumer的并发控制
4. RunnablePoller的ScheduledCommand支持

### 中优先级（性能优化）

1. WorkflowStepCollection和ExecutionPointerCollection的O(1)查找优化
2. IndexConsumer的对象池和错误重试
3. WorkflowConsumer的ScheduledCommand完善

### 低优先级（辅助功能）

1. 默认提供者实现
2. WorkflowHost的公开属性
3. 工具类完善

## 四、注意事项

1. **保持向后兼容**：在优化集合类时，确保现有代码仍能正常工作
2. **测试覆盖**：每个任务完成后需要验证功能正确性
3. **代码风格**：遵循项目现有的代码风格和命名规范
4. **性能考虑**：O(1)查找优化对大量步骤/指针的场景很重要
5. **线程安全**：确保所有共享资源的访问是线程安全的