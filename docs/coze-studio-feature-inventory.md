# Coze Studio 功能点清单

> 文档生成时间：2026-03-13  
> 分析项目：`D:\Code\coze-studio`  
> 数据来源：后端 Application/Domain/Infra 层代码、前端路由/页面/组件代码、配置文件

---

## 一、项目主要功能模块

| 模块 | 说明 | 后端包 | 前端包 |
|------|------|--------|--------|
| **用户认证** | 注册、登录、登出、密码重置、Session 管理 | `application/user` | `foundation/account` |
| **工作空间** | Space 管理、开发列表、资源库 | `application/user`, `application/search` | `foundation/space`, `studio/workspace` |
| **Agent (Bot) 管理** | Agent 创建/编辑/复制/删除/发布 | `application/singleagent` | `agent-ide` |
| **对话系统** | Agent 对话 (SSE)、消息管理 | `application/conversation` | `common/chat-area` |
| **工作流引擎** | 可视化工作流编辑/运行/调试/发布 | `application/workflow`, `domain/workflow` | `workflow` |
| **知识库** | 知识库 CRUD、文档处理、RAG 检索 | `application/knowledge` | `data/knowledge` |
| **数据库 (Memory)** | 数据库 CRUD、记录管理、导入 | `application/memory` | `data/memory` |
| **变量系统** | KV 变量、项目变量、Playground 内存 | `application/memory` | `agent-ide`, `data/memory` |
| **插件系统** | 插件注册/开发/调试/发布、OAuth | `application/plugin` | `studio/plugin-forms` |
| **应用/项目 (App)** | App 创建/编辑/复制/删除/发布 | `application/app` | `project-ide` |
| **Prompt 管理** | Prompt 模板 CRUD、官方 Prompt 库 | `application/prompt` | `common/prompt-kit` |
| **模型管理** | LLM 模型配置、多 Provider 支持 | `application/modelmgr`, `bizpkg/config` | `agent-ide/model-manager` |
| **市场/探索** | 插件/模板商店、搜索、收藏 | `application/plugin`, `application/search` | `community/explore` |
| **上传系统** | 文件/图片上传、分片上传、ImageX | `application/upload` | 多模块 |
| **权限/PAT** | Personal Access Token CRUD、API 鉴权 | `application/openauth` | `foundation/account` |
| **管理后台** | 基础配置、知识库配置、模型管理 | `bizpkg/config` | `/admin` 静态页面 |
| **搜索系统** | 全局搜索、ES 索引同步 | `application/search`, `infra/es` | `community/search` |
| **DevOps/调试** | 测试集、Mock、Trace 可视化 | — | `devops` |
| **代码沙箱** | Python 代码节点执行 | `infra/coderunner` | — |
| **文档处理** | PDF/DOCX 解析、OCR、向量化 | `infra/document` | — |

---

## 二、每个模块下的功能点

---

### 2.1 用户认证 (Passport)

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 邮箱注册 | 邮箱 + 密码注册新用户 | `application/user/`, `idl/passport/passport.thrift` | `PassportWebEmailRegisterV2` | ✅ 完整 |
| 邮箱登录 | 邮箱 + 密码登录，生成 Session | 同上 | `PassportWebEmailLoginPost` | ✅ 完整 |
| 登出 | 清除 Session | 同上 | `PassportWebLogoutGet` | ✅ 完整 |
| 密码重置 | 重置用户密码 | 同上 | `PassportWebEmailPasswordResetGet` | ✅ 完整 |
| Session 校验 | Cookie session_key 校验 | `api/middleware/session.go` | `ValidateSession` | ✅ 完整 |
| 账号信息 | 获取当前用户信息 | 同上 | `PassportAccountInfoV2` | ✅ 完整 |
| 资料更新 | 修改用户名等资料 | 同上 | `UserUpdateProfile` | ✅ 完整 |
| 头像上传 | 上传用户头像 | 同上 | `UserUpdateAvatar` | ✅ 完整 |
| 资料检查 | 更新前检查是否可修改 | 同上 | `UpdateUserProfileCheck` | ✅ 完整 |

**前端页面**：`/sign` 登录页、Account Settings 面板

---

### 2.2 工作空间 (Space)

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| Space 列表 | 获取用户空间列表 | `application/user/` | `GetSpaceListV2` | ✅ 完整 |
| 开发列表 | 展示 Agent/App/项目列表，支持过滤搜索 | `application/search/` | `GetDraftIntelligenceList` | ✅ 完整 |
| 资源库 | 插件/工作流/知识库/Prompt/数据库资源 | `application/search/` | `LibraryResourceList`, `ProjectResourceList` | ✅ 完整 |
| 最近编辑 | 最近编辑的智能体/项目 | `application/search/` | `GetUserRecentlyEditIntelligence` | ✅ 完整 |
| 全局搜索 | 搜索资源、Agent、项目 | `infra/es/` | ES 索引搜索 | ✅ 完整 |

**前端路由**：`/space/:space_id/develop` (开发), `/space/:space_id/library` (资源库), `/search/:word` (搜索)

---

### 2.3 Agent (Bot) 管理

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 创建 Agent | 创建草稿 Agent，自动设置默认模型 | `application/singleagent/` | `CreateSingleAgentDraft` | ✅ 完整 |
| 编辑 Agent | 更新名称/模型/插件/知识库/变量 | 同上 | `UpdateSingleAgentDraft` | ✅ 完整 |
| 删除 Agent | 删除草稿 Agent + 发布事件 | 同上 | `DeleteAgentDraft` | ✅ 完整 |
| 复制 Agent | 复制 Agent (含变量/插件/快捷命令) | 同上 | `DuplicateDraftBot` | ✅ 完整 |
| Agent 信息 | 获取完整 Agent 信息 (知识库/插件/工作流/快捷命令) | 同上 | `GetAgentBotInfo` | ✅ 完整 |
| 发布 Agent | 并行发布到多个 Connector | 同上 | `PublishAgent` | ✅ 完整 |
| 发布历史 | 查看发布历史记录 | 同上 | `ListAgentPublishHistory` | ✅ 完整 |
| 发布渠道 | 获取已发布渠道列表 | 同上 | `GetPublishConnectorList` | ✅ 完整 |
| Agent 在线信息 | Open API 获取在线 Agent 信息 | 同上 | `GetAgentOnlineInfo`, `OpenGetBotInfo` | ✅ 完整 |
| Agent 展示信息 | 获取/更新 Agent 展示信息 | 同上 | `Get/UpdateAgentDraftDisplayInfo` | ✅ 完整 |
| 弹窗计数 | Agent 弹窗信息管理 | 同上 | `Get/UpdateAgentPopupInfo` | ✅ 完整 |
| 行为上报 | 上报最近打开记录 | 同上 | `ReportUserBehavior` | ✅ 完整 |
| 绑定数据库 | 绑定/解绑数据库到 Agent | 同上 | `BindDatabase`, `UnBindDatabase` | ✅ 完整 |
| Prompt 禁用 | 切换数据库 Prompt 开关 | 同上 | `UpdatePromptDisable` | ✅ 完整 |
| 模式选择 | Single LLM / Workflow 模式切换 | 前端 `agent-ide/entry` | — | ✅ 完整 |

**前端路由**：`/space/:space_id/bot/:bot_id` (Agent IDE), `/space/:space_id/bot/:bot_id/publish` (发布)

---

### 2.4 对话系统

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| Agent 对话 (内部) | Playground SSE 流式对话 | `application/conversation/` | `Run` | ✅ 完整 |
| Agent 对话 (Open API) | v3 Chat SSE/同步对话 | 同上 | `OpenapiAgentRun`, `OpenapiAgentRunSync` | ✅ 完整 |
| 取消对话 | 取消进行中的对话 | 同上 | `CancelRun` | ✅ 完整 |
| 获取运行记录 | 查看对话运行状态 | 同上 | `RetrieveRunRecord` | ✅ 完整 |
| 创建会话 | 创建新会话 | 同上 | `CreateConversation` | ✅ 完整 |
| 会话列表 | 列出会话 | 同上 | `ListConversation` | ✅ 完整 |
| 更新/删除会话 | 会话 CRUD | 同上 | `Update/DeleteConversation` | ✅ 完整 |
| 消息列表 | 查看消息列表 | 同上 | `GetMessageList`, `GetApiMessageList` | ✅ 完整 |
| 删除消息 | 删除消息 | 同上 | `DeleteMessage` | ✅ 完整 |
| 中断消息 | 中断消息生成 | 同上 | `BreakMessage` | ✅ 完整 |
| 清除上下文 | 创建新分段 | 同上 | `CreateSection` | ✅ 完整 |
| 清除历史 | 清空会话历史 | 同上 | `ClearHistory` | ✅ 完整 |

**前端组件**：`common/chat-area`（聊天面板、消息列表、快捷命令、引用回复）

---

### 2.5 工作流引擎

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 创建工作流 | 创建新工作流 | `application/workflow/` | `CreateWorkflow` | ✅ 完整 |
| 保存工作流 | 保存画布和节点配置 | 同上 | `SaveWorkflow` | ✅ 完整 |
| 工作流详情 | 获取工作流详情/画布 | 同上 | `GetWorkflowDetail`, `GetCanvasInfo` | ✅ 完整 |
| 删除工作流 | 单个/批量删除 | 同上 | `DeleteWorkflow`, `BatchDeleteWorkflow` | ✅ 完整 |
| 复制工作流 | 复制工作流/模板 | 同上 | `CopyWorkflow`, `CopyWkTemplateApi` | ✅ 完整 |
| 发布工作流 | 发布到线上版本 | 同上 | `PublishWorkflow` | ✅ 完整 |
| 测试运行 | 在 Playground 测试运行 | 同上 | `TestRun` | ✅ 完整 |
| 测试恢复 | 从中断点恢复运行 | 同上 | `TestResume` | ✅ 完整 |
| 取消运行 | 取消进行中的工作流 | 同上 | `Cancel` | ✅ 完整 |
| 节点调试 | 单节点调试 (使用缓存输入) | 同上 | `NodeDebug` | ✅ 完整 |
| 运行进度 | 获取工作流运行进度 | 同上 | `GetProcess` | ✅ 完整 |
| 节点执行历史 | 查看节点执行历史 | 同上 | `GetNodeExecuteHistory` | ✅ 完整 |
| 校验工作流 | 验证 DAG 结构 | 同上 | `ValidateTree` | ✅ 完整 |
| 引用查询 | 查询工作流引用关系 | 同上 | `GetWorkflowReferences` | ✅ 完整 |
| 节点类型查询 | 查询支持的节点类型 | 同上 | `QueryWorkflowNodeTypes` | ✅ 完整 |
| 节点模板 | 获取节点模板列表 | 同上 | `GetNodeTemplateList` | ✅ 完整 |
| LLM FC 设置 | 大模型函数调用设置 | 同上 | `GetLLMNodeFCSettingDetail/Merged` | ✅ 完整 |
| ChatFlow 角色 | Chat 工作流角色管理 | 同上 | `Create/Delete/GetChatFlowRole` | ✅ 完整 |
| 项目对话定义 | 项目对话模板 CRUD | 同上 | `Create/Update/Delete/ListApplicationConversationDef` | ✅ 完整 |
| Trace 追踪 | 工作流运行追踪 | 同上 | `GetTraceSDK`, `ListRootSpans` | ✅ 完整 |
| Open API 运行 | 外部 API 触发工作流 | 同上 | `OpenAPIRun`, `OpenAPIStreamRun` | ✅ 完整 |
| ChatFlow 运行 | Chat 模式工作流运行 | 同上 | `OpenAPIChatFlowRun` | ✅ 完整 |
| 示例工作流 | 示例工作流列表 | 同上 | `GetExampleWorkFlowList` | ✅ 完整 |

**支持 47 种节点类型**（详见下方节点列表）

**前端路由**：`/work_flow` (工作流编辑器)，支持画布拖拽、节点连接、变量系统、测试运行面板

---

### 2.6 工作流节点类型 (47 种)

| 分类 | 节点 | 说明 |
|------|------|------|
| **输入输出** | Entry, Exit, InputReceiver, OutputEmitter | 工作流入口/出口/输入/输出 |
| **AI/LLM** | LLM | 大语言模型调用 |
| **逻辑控制** | Selector (条件分支), Loop (循环), Batch (批处理), Break, Continue, IntentDetector (意图识别) | 流程控制 |
| **变量** | VariableAssigner, VariableAssignerWithinLoop, VariableAggregator | 变量赋值/合并 |
| **数据** | KnowledgeRetriever (知识检索), KnowledgeIndexer (知识索引), KnowledgeDeleter (知识删除), Plugin | RAG + 插件 |
| **数据库** | DatabaseQuery, DatabaseInsert, DatabaseUpdate, DatabaseDelete, DatabaseCustomSQL | 数据库操作 |
| **工具** | HTTPRequester (HTTP 请求), CodeRunner (代码执行), TextProcessor (文本处理), SubWorkflow (子工作流) | 工具节点 |
| **JSON** | JsonSerialization, JsonDeserialization | JSON 序列化/反序列化 |
| **对话管理** | CreateConversation, ConversationUpdate, ConversationDelete, ConversationList, ConversationHistory, ClearConversationHistory | 对话操作 |
| **消息** | MessageList, CreateMessage, EditMessage, DeleteMessage | 消息操作 |
| **其他** | QuestionAnswer (用户提问), Comment (注释), Lambda (占位) | 辅助 |

---

### 2.7 知识库

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 创建知识库 | 支持文本/表格/图片知识库 | `application/knowledge/` | `CreateKnowledge` | ✅ 完整 |
| 知识库列表 | 分页列表 | 同上 | `ListKnowledge` | ✅ 完整 |
| 知识库详情 | 查看详细信息 | 同上 | `DatasetDetail` | ✅ 完整 |
| 更新/删除 | 知识库 CRUD | 同上 | `UpdateKnowledge`, `DeleteKnowledge` | ✅ 完整 |
| 创建文档 | 上传并解析文档 | 同上 | `CreateDocument` | ✅ 完整 |
| 文档列表 | 查看文档列表 | 同上 | `ListDocument` | ✅ 完整 |
| 文档进度 | 查看文档处理进度 | 同上 | `GetDocumentProgress` | ✅ 完整 |
| 重新分段 | 批量重新分段文档 | 同上 | `Resegment` | ✅ 完整 |
| 片段 CRUD | 创建/查看/修改/删除片段 | 同上 | `Create/List/Update/DeleteSlice` | ✅ 完整 |
| 表 Schema | 获取/校验表 Schema | 同上 | `GetTableSchema`, `ValidateTableSchema` | ✅ 完整 |
| 文档审核 | 文档审核流程 | 同上 | `CreateDocumentReview`, `SaveDocumentReview` | ✅ 完整 |
| 图片知识库 | 图片列表/详情/描述生成 | 同上 | `ListPhoto`, `ExtractPhotoCaption` | ✅ 完整 |
| 复制/移动 | 知识库跨项目复制/移动 | 同上 | `CopyKnowledge`, `MoveKnowledgeToLibrary` | ✅ 完整 |
| Open API | 外部 API CRUD 知识库/文档 | 同上 | `*OpenAPI` 方法 | ✅ 完整 |

**文档处理能力**（`infra/document/`）：

| 能力 | 支持格式 | 实现 |
|------|----------|------|
| 文档解析 | PDF, TXT, DOC/DOCX, Markdown, CSV, XLSX, JSON | ✅ |
| 图片处理 | JPG, JPEG, PNG | ✅ |
| OCR | Volcengine OCR, PaddleOCR | ✅ |
| 重排序 | VikingDB Rerank, RRF | ✅ |
| NL2SQL | LLM + Jinja2 模板 | ✅ |
| 向量索引 | VikingDB, OceanBase | ✅ |

**前端路由**：`/space/:space_id/knowledge/:dataset_id` (预览), `/space/:space_id/knowledge/:dataset_id/upload` (上传)

---

### 2.8 数据库 (Memory)

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 数据库 CRUD | 创建/查看/更新/删除数据库 | `application/memory/` | `Add/Update/DeleteDatabase` | ✅ 完整 |
| 记录管理 | 查询/新增/修改/删除记录 | 同上 | `ListDatabaseRecords`, `UpdateDatabaseRecords` | ✅ 完整 |
| 绑定 Bot | 绑定/解绑数据库到 Bot | 同上 | `BindDatabase`, `UnBindDatabase` | ✅ 完整 |
| 表 Schema | 获取/校验表结构 | 同上 | `GetDatabaseTableSchema`, `ValidateDatabaseTableSchema` | ✅ 完整 |
| 数据导入 | 异步文件导入任务 | 同上 | `SubmitDatabaseInsertTask` | ✅ 完整 |
| 导入进度 | 查看导入进度 | 同上 | `DatabaseFileProgressData` | ✅ 完整 |
| 模板导出 | 导出数据库模板 (TOS URL) | 同上 | `GetDatabaseTemplate` | ✅ 完整 |
| 复制/移动 | 数据库跨项目复制/移动 | 同上 | `CopyDatabase`, `MoveDatabaseToLibrary` | ✅ 完整 |
| 模式配置 | 获取模式配置 | 同上 | `GetModeConfig` | ⚠️ 硬编码返回 |
| 连接器名称 | 获取连接器列表 | 同上 | `GetConnectorName` | ⚠️ 硬编码返回 |

**前端路由**：`/space/:space_id/database/:table_id` (数据库详情)

---

### 2.9 变量系统

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 系统变量配置 | 获取系统预定义变量 | `application/memory/` | `GetSysVariableConf` | ✅ 完整 |
| 项目变量 | 获取/更新项目级变量 | 同上 | `GetProjectVariablesMeta`, `UpdateProjectVariable` | ✅ 完整 |
| 变量实例 | 设置/获取/删除 KV 变量 | 同上 | `SetVariableInstance`, `GetPlayGroundMemory`, `DeleteVariableInstance` | ✅ 完整 |
| 变量元数据 | 获取变量定义信息 | 同上 | `GetVariableMeta` | ✅ 完整 |

---

### 2.10 插件系统

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 注册插件 | 注册新插件 + 元数据 | `application/plugin/` | `RegisterPlugin`, `RegisterPluginMeta` | ✅ 完整 |
| 插件信息 | 获取插件详情 | 同上 | `GetPluginInfo` | ✅ 完整 |
| 更新插件 | 更新插件/元数据 | 同上 | `UpdatePlugin`, `UpdatePluginMeta` | ✅ 完整 |
| 删除插件 | 删除插件 | 同上 | `DelPlugin` | ✅ 完整 |
| 发布插件 | 发布到商店 | 同上 | `PublishPlugin` | ✅ 完整 |
| API CRUD | 创建/更新/删除/批量创建 API | 同上 | `CreateAPI`, `UpdateAPI`, `DeleteAPI`, `BatchCreateAPI` | ✅ 完整 |
| API 调试 | 在线调试 API | 同上 | `DebugAPI` | ✅ 完整 |
| OpenAPI 转换 | 转换为 OpenAPI 格式 | 同上 | `Convert2OpenAPI` | ✅ 完整 |
| OAuth 流程 | OAuth Schema/状态/授权码 | 同上 | `GetOAuthSchema`, `GetOAuthStatus`, `OauthAuthorizationCode` | ✅ 完整 |
| 编辑锁 | 检查/锁定/解锁编辑 | 同上 | `CheckAndLockPluginEdit`, `UnlockPluginEdit` | ⚠️ Stub (总是返回 true) |
| Bot 默认参数 | 获取/更新 Bot 默认工具参数 | 同上 | `GetBotDefaultParams`, `UpdateBotDefaultParams` | ✅ 完整 |
| 资源复制 | 资源复制派发/详情/重试/取消 | `application/app/` | `ResourceCopyDispatch/Detail/Retry/Cancel` | ✅ 完整 |

**22 个内置插件**：文库搜索、博查搜索、Wolfram Alpha、创客贴设计、搜狐热闻、图片压缩、什么值得买、板栗看板、天眼查、飞书 (认证/消息/多维表格/电子表格/任务/云文档/知识库/日历)、高德地图

**前端路由**：`/space/:space_id/plugin/:plugin_id` (插件详情), `/space/:space_id/plugin/:plugin_id/tool/:tool_id` (工具编辑)

---

### 2.11 应用/项目 (App)

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 创建项目 | 创建 App 草稿项目 | `application/app/` | `DraftProjectCreate` | ✅ 完整 |
| 更新/删除项目 | 项目 CRUD (删除含异步资源清理) | 同上 | `DraftProjectUpdate`, `DraftProjectDelete` | ✅ 完整 |
| 复制项目 | 复制项目及所有资源 | 同上 | `DraftProjectCopy` | ✅ 完整 |
| 项目信息 | 获取草稿智能体信息 | 同上 | `GetDraftIntelligenceInfo` | ✅ 完整 |
| 发布项目 | 发布 App 到渠道 | 同上 | `PublishAPP` | ✅ 完整 |
| 版本检查 | 检查版本号 | 同上 | `CheckProjectVersionNumber` | ✅ 完整 |
| 发布记录 | 发布历史列表和详情 | 同上 | `GetPublishRecordList/Detail` | ✅ 完整 |
| 发布渠道 | 获取可用/已发布渠道 | 同上 | `ProjectPublishConnectorList`, `GetProjectPublishedConnector` | ✅ 完整 |
| 内部任务列表 | 项目内任务列表 | 同上 | `DraftProjectInnerTaskList` | ✅ 完整 |
| 在线应用数据 | 获取在线 App 数据 (Open API) | 同上 | `GetOnlineAppData` | ✅ 完整 |
| 行为上报 | 上报最近使用 | 同上 | `ReportUserBehavior` | ✅ 完整 |

**前端路由**：`/space/:space_id/project-ide/:project_id/*` (项目 IDE), 具有 VS Code 风格的活动栏、标签、侧边栏

---

### 2.12 Prompt 管理

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 创建/更新 Prompt | Upsert Prompt 资源 + 发布事件 | `application/prompt/` | `UpsertPromptResource` | ✅ 完整 |
| Prompt 信息 | 获取 Prompt 详情 | 同上 | `GetPromptResourceInfo` | ✅ 完整 |
| 官方 Prompt 列表 | 获取官方 Prompt 模板库 | 同上 | `GetOfficialPromptResourceList` | ✅ 完整 |
| 删除 Prompt | 删除 + 发布事件 | 同上 | `DeletePromptResource` | ✅ 完整 |

**前端组件**：`common/prompt-kit`（Prompt 库、推荐面板、创建/编辑模态框、变量插入）

---

### 2.13 模型管理

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 模型列表 | 获取已配置模型列表 | `application/modelmgr/` | `GetModelList` | ✅ 完整 |
| 创建模型 | 添加模型配置 (含连通性测试) | `bizpkg/config/modelmgr/` | `CreateModel` | ✅ 完整 |
| 删除模型 | 删除模型配置 | 同上 | `DeleteModel` | ✅ 完整 |
| 获取 Provider 列表 | 获取模型提供商列表 | 同上 | `GetProviderModelList` | ✅ 完整 |

**支持 LLM Provider**：OpenAI, Claude, Gemini, Qwen, DeepSeek, Ollama, Ark (ByteDance), BytePlus Seed  
**22 个模型模板 YAML 文件**

---

### 2.14 市场/探索 (Marketplace)

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 产品列表 | 浏览市场产品 | `application/plugin/` | `PublicGetProductList` | ✅ 完整 |
| 产品详情 | 查看产品详情 | 同上 | `PublicGetProductDetail` | ✅ 完整 |
| 搜索产品 | 搜索市场产品 | 同上 | `PublicSearchProduct` | ✅ 完整 |
| 搜索建议 | 搜索自动补全 | 同上 | `PublicSearchSuggest` | ✅ 完整 |
| 分类列表 | 产品分类浏览 | 同上 | `PublicGetProductCategoryList` | ✅ 完整 |
| 收藏 | 收藏/取消收藏产品 | `application/search/` | `PublicFavoriteProduct` | ✅ 完整 |
| 收藏列表 | 查看收藏列表 | 同上 | `PublicGetUserFavoriteList` | ✅ 完整 |
| 复制产品 | 从市场复制到工作区 | 同上 | `PublicDuplicateProduct` | ✅ 完整 |

**前端路由**：`/explore/plugin` (插件商店), `/explore/template` (模板商店)

---

### 2.15 上传系统

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 通用文件上传 | 上传文件到对象存储 | `application/upload/` | `UploadFileCommon` | ✅ 完整 |
| 分片上传 | 大文件分片上传 (Init/Part/Complete) | 同上 | `PartUploadFileInit/PartUploadFile/PartUploadFileComplete` | ✅ 完整 |
| 图片上传 | 图片上传 (Apply/Commit) | 同上 | `ApplyImageUpload`, `CommitImageUpload` | ✅ 完整 |
| Open API 上传 | 外部 API 文件上传 | 同上 | `UploadFileOpen` | ✅ 完整 |
| 图标获取 | 获取资源图标 | 同上 | `GetIcon`, `GetIconForDataset`, `GetShortcutIcons` | ✅ 完整 |
| Session 上传 | 基于 Session Key 的上传 | 同上 | `UploadSessionKey`, `GetObjInfoBySessionKey` | ✅ 完整 |

**存储后端**：MinIO (开发), TOS, S3 — 由 `STORAGE_TYPE` 环境变量控制

---

### 2.16 权限/PAT (Personal Access Token)

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 创建 PAT | 创建个人访问令牌 + 权限 | `application/openauth/` | `CreatePersonalAccessToken` | ✅ 完整 |
| PAT 列表 | 查看令牌列表 | 同上 | `ListPersonalAccessTokens` | ✅ 完整 |
| PAT 详情 | 获取令牌详情和权限 | 同上 | `GetPersonalAccessTokenAndPermission` | ✅ 完整 |
| 更新 PAT | 更新令牌权限 | 同上 | `UpdatePersonalAccessTokenAndPermission` | ✅ 完整 |
| 删除 PAT | 删除令牌 | 同上 | `DeletePersonalAccessTokenAndPermission` | ✅ 完整 |
| 权限校验 | API 请求权限检查 | 同上 | `CheckPermission` | ✅ 完整 |
| 模拟用户 | 临时模拟 Coze 用户 | 同上 | `ImpersonateCozeUserAccessToken` | ✅ 完整 |

---

### 2.17 管理后台 (Admin)

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 基础配置 | 获取/保存平台基础配置 | `bizpkg/config/` | `GetBaseConfig`, `SaveBaseConfig` | ✅ 完整 |
| 知识库配置 | 配置知识库全局参数 | 同上 | `GetKnowledgeConfig`, `SaveKnowledgeConfig` | ✅ 完整 |
| 模型管理 | 模型 CRUD (含连通性验证) | 同上 | `GetProviderModelList`, `CreateModel`, `DeleteModel` | ✅ 完整 |

**访问控制**：Session + AdminEmails 白名单  
**前端入口**：`/admin` (静态配置页面 `resources/conf`)

---

### 2.18 DevOps / 调试 (前端)

| 功能名称 | 功能说明 | 涉及文件 | 完成度 |
|----------|----------|----------|--------|
| 测试集管理 | 创建/编辑/删除测试集 | `frontend/packages/devops/testset/` | ✅ 完整 |
| Mock 集管理 | 创建/编辑/删除 Mock 数据集 | `frontend/packages/devops/mockset/` | ✅ 完整 |
| 调试面板 | Trace 树可视化、Span 详情、Chart | `frontend/packages/devops/debug/` | ✅ 完整 |
| JSON 预览 | PDF/图片/JSON 预览 | `frontend/packages/devops/json-link-preview/` | ✅ 完整 |

---

### 2.19 代码沙箱 (Code Runner)

| 功能名称 | 功能说明 | 涉及文件 | 完成度 |
|----------|----------|----------|--------|
| Python 直接执行 | exec.Command 执行 Python 代码 | `infra/coderunner/impl/direct/` | ✅ 完整 |
| Python 沙箱执行 | sandbox.py 隔离执行 | `infra/coderunner/impl/sandbox/` | ✅ 完整 |
| JavaScript 执行 | JS 代码节点 | `infra/coderunner/` | ❌ 未实现 (返回 "js not supported yet") |

**第三方模块白名单**：`httpx`, `numpy`

---

### 2.20 事件总线 & 后台消费者

| 功能名称 | 功能说明 | 涉及文件 | 完成度 |
|----------|----------|----------|--------|
| 资源搜索同步 | 资源事件 → ES 索引同步 | `infra/eventbus/handler_resource` | ✅ 完整 |
| 项目搜索同步 | 项目事件 → ES 索引同步 | `infra/eventbus/handler_project` | ✅ 完整 |
| 知识库索引 | 知识库事件 → 向量索引 | `infra/eventbus/knowledgeEventHandler` | ✅ 完整 |

**Topics**：`opencoze_search_app`, `opencoze_search_resource`, `opencoze_knowledge`  
**支持 MQ**：NSQ / Kafka / RocketMQ / Pulsar / NATS

---

### 2.21 快捷命令

| 功能名称 | 功能说明 | 涉及文件 | 关键方法 | 完成度 |
|----------|----------|----------|----------|--------|
| 创建/更新快捷命令 | Agent 快捷命令管理 | `application/shortcutcmd/` | `Handler` | ✅ 完整 |

---

### 2.22 Open Platform (SDK 集成)

| 功能名称 | 功能说明 | 涉及文件 | 完成度 |
|----------|----------|----------|--------|
| Chat SDK | Web Chat SDK 集成 | `frontend/packages/studio/open-platform/` | ✅ 完整 |
| Chat App | `/chat`, `/app_widget`, `/client` | `frontend/packages/studio/dev-app/` | ✅ 完整 |
| Web SDK Guide | 发布后的 SDK 接入引导 | `frontend/packages/studio/workspace/publish/` | ✅ 完整 |

---

## 三、暂时无法确认的功能点

| 功能点 | 说明 | 依据 |
|--------|------|------|
| **定时任务 (Scheduled Tasks)** | IDL 和数据模型中有 `CronExpr`、`ScheduledTaskTabStatus` 字段，但后端无 cron/scheduler 实现 | `api/model/app/bot_common/bot_common.go`, `domain/agent/` 中有引用 |
| **编辑锁 (Optimistic Locking)** | `CheckAndLockPluginEdit` 始终返回 `Seized: true`，`UnlockPluginEdit` 始终返回 `Released: true` | 可能预留为分布式锁场景 |
| **连接器名称** | `GetConnectorName` 硬编码返回 Coze/Chat SDK/API 三个固定值 | 可能预留更多 Connector 类型 |
| **模式配置** | `GetModeConfig` 硬编码返回固定配置 | 可能预留为可配置的 |
| **JavaScript 代码节点** | Code Runner 中定义了 JS 但返回 "not supported yet" | 预留功能 |
| **VikingDB 向量搜索** | 实现了 VikingDB 搜索存储但依赖外部服务 | 需要 VikingDB 服务配置 |
| **Webhook / 回调** | 未发现 Webhook 推送机制 | 仅有 Connector 发布概念 |
| **多租户隔离** | 单 Space 结构，无显式租户隔离 | 数据按 SpaceID 隔离 |
| **审计日志** | 未发现操作审计日志模块 | 仅有 AccessLog 中间件 |
| **限流/熔断** | 未发现 Rate Limiting 或 Circuit Breaker | CORS 全开，无限流 |

---

## 四、最核心的 10 个功能

| 排名 | 功能 | 核心度说明 |
|------|------|------------|
| **1** | **工作流引擎** | 47 种节点，可视化编辑/测试/调试/发布，DAG 验证，SSE 流式执行，ChatFlow 模式，是平台最核心的差异化能力 |
| **2** | **Agent (Bot) 对话** | SSE 流式 Agent 对话，支持内部 Playground + Open API (v3 Chat)，是用户最直接的交互入口 |
| **3** | **知识库 RAG 系统** | 支持文本/表格/图片知识库，PDF/DOCX/OCR 解析，向量检索 (Milvus/VikingDB)，NL2SQL，Rerank |
| **4** | **Agent 管理** | Agent 全生命周期管理 (创建/编辑/复制/发布)，绑定模型/插件/知识库/数据库/变量，是平台的核心实体 |
| **5** | **插件系统** | 插件注册/开发/调试/发布，22 个内置插件，OAuth 集成，OpenAPI 转换，是扩展性基础 |
| **6** | **多模型支持** | 8+ LLM Provider (OpenAI/Claude/Gemini/Qwen/DeepSeek/Ollama/Ark/BytePlus)，22 个模型模板，统一抽象 |
| **7** | **数据库/变量系统** | 结构化数据存储 + KV 变量 + 项目变量，支持工作流节点操作，是 Agent "记忆"的基础 |
| **8** | **Open API 体系** | v1/v3 外部集成 API，PAT 认证，覆盖 Chat/Conversation/Workflow/Knowledge/File，是商业化接入基础 |
| **9** | **App/项目管理** | 复合 App 管理 (包含 Agent + Workflow + Plugin + Knowledge + Database)，VS Code 风格项目 IDE |
| **10** | **事件驱动搜索** | MQ 异步事件 → ES 索引同步 + 知识库向量索引，支撑全局搜索和 RAG 检索的实时性 |

---

## 附录：错误码分布

| 模块 | 错误码数量 | 文件 |
|------|-----------|------|
| Workflow | ~49 | `types/errno/workflow.go` |
| Knowledge | ~38 | `types/errno/knowledge.go` |
| Memory | ~27 | `types/errno/memory.go` |
| Plugin | ~22 | `types/errno/plugin.go` |
| Agent | ~14 | `types/errno/agent.go` |
| Conversation | ~14 | `types/errno/conversation.go` |
| Upload | ~11 | `types/errno/upload.go` |
| User | ~9 | `types/errno/user.go` |
| Prompt | ~8 | `types/errno/prompt.go` |
| App | ~4 | `types/errno/app.go` |
| Connector | ~3 | `types/errno/connector.go` |
| Search | ~2 | `types/errno/search.go` |
| ModelMgr | ~2 | `types/errno/modelmgr.go` |
| ShortcutCmd | ~2 | `types/errno/shortcutcmd.go` |
| Permission | ~1 | `types/errno/permission.go` |
| **合计** | **~206** | |
