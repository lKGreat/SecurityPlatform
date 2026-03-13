# Coze Studio 接口清单

> 文档生成时间：2026-03-13  
> 分析项目：`D:\Code\coze-studio`  
> 数据来源：Thrift IDL 定义 (`idl/`)、路由注册 (`backend/api/router/`)、Handler 实现 (`backend/api/handler/`)

---

## 目录

- [一、接口总览](#一接口总览)
- [二、认证鉴权机制](#二认证鉴权机制)
- [三、对外接口清单（按模块）](#三对外接口清单按模块)
  - [3.1 管理后台 (Admin)](#31-管理后台-admin)
  - [3.2 用户认证 (Passport)](#32-用户认证-passport)
  - [3.3 用户管理 (User)](#33-用户管理-user)
  - [3.4 Agent/Bot 管理 (DraftBot)](#34-agentbot-管理-draftbot)
  - [3.5 对话 (Conversation)](#35-对话-conversation)
  - [3.6 消息 (Message)](#36-消息-message)
  - [3.7 知识库 (Knowledge)](#37-知识库-knowledge)
  - [3.8 数据库/变量 (Memory/Database)](#38-数据库变量-memorydatabase)
  - [3.9 工作流 (Workflow)](#39-工作流-workflow)
  - [3.10 插件 (Plugin)](#310-插件-plugin)
  - [3.11 应用/项目 (Intelligence/App)](#311-应用项目-intelligenceapp)
  - [3.12 市场 (Marketplace)](#312-市场-marketplace)
  - [3.13 Playground](#313-playground)
  - [3.14 上传 (Upload)](#314-上传-upload)
  - [3.15 权限/PAT (Permission)](#315-权限pat-permission)
  - [3.16 OAuth](#316-oauth)
- [四、Open API（v1/v3 外部集成接口）](#四open-apiv1v3-外部集成接口)
  - [4.1 Chat v3](#41-chat-v3)
  - [4.2 Conversation v1](#42-conversation-v1)
  - [4.3 Workflow v1](#43-workflow-v1)
  - [4.4 Knowledge/Dataset v1](#44-knowledgedataset-v1)
  - [4.5 Bot v1](#45-bot-v1)
  - [4.6 App v1](#46-app-v1)
  - [4.7 File v1](#47-file-v1)
  - [4.8 Knowledge Document (open_api)](#48-knowledge-document-open_api)
- [五、文件上传下载接口](#五文件上传下载接口)
- [六、SSE 流式接口](#六sse-流式接口)
- [七、内部服务调用清单](#七内部服务调用清单)
- [八、消息队列事件](#八消息队列事件)
- [九、接口统计](#九接口统计)

---

## 一、接口总览

| 维度 | 数量 |
|------|------|
| Thrift IDL Service 定义 | 18 个 |
| HTTP API 接口总数 | ~240+ |
| 内部 Web API (`/api/`) | ~175 |
| 外部 Open API (`/v1/`, `/v3/`, `/open_api/`) | ~30 |
| IDL 文件 | 49 个 (.thrift) |
| 路径前缀 | `/api/`, `/v1/`, `/v3/`, `/open_api/` |

**HTTP 框架**：CloudWeGo Hertz (v0.10.2)  
**路由生成**：Thrift IDL → Hertz 代码生成  
**参数绑定**：`c.BindAndValidate(&req)` (支持 path/query/json/form tag)

---

## 二、认证鉴权机制

### 中间件执行顺序

```
1. ContextCacheMW()         → 初始化请求级缓存 (必须第一个)
2. RequestInspectorMW()     → 判定认证类型 (WebAPI / OpenAPI / StaticFile)
3. SetHostMW()              → 存储 Host 和 Scheme
4. SetLogIDMW()             → 生成 UUID Log ID，写入 X-Log-ID 响应头
5. CORS                     → 允许所有来源 (AllowAllOrigins)
6. AccessLogMW()            → 请求日志 (5xx=error, 4xx=warn, 2xx=info)
7. OpenapiAuthMW()          → Bearer Token (PAT) 认证
8. SessionAuthMW()          → Cookie Session 认证
9. I18nMW()                 → 国际化 (Accept-Language / Session.Locale)
```

### 三种认证类型

| 类型 | 触发条件 | 认证方式 | 适用路径 |
|------|----------|----------|----------|
| **WebAPI** | 默认 (非 OpenAPI、非 Static) | Cookie `session_key` → `ValidateSession()` | `/api/*` (除 login/register) |
| **OpenAPI** | 路径匹配 `needAuthPath` | `Authorization: Bearer <PAT>` → MD5 → `CheckPermission()` | `/v1/*`, `/v3/*`, `/open_api/*` |
| **StaticFile** | 路径匹配静态文件规则 | 无需认证 | `/`, `/static/*`, `/sign`, `/favicon.png`, `/admin/*`, `/space/*` |
| **Admin** | 路由组 `/api/admin/*` | Session + AdminEmails 白名单 | `/api/admin/config/*` |

### Session 认证免认证路径

- `POST /api/passport/web/email/login/`
- `POST /api/passport/web/email/register/v2/`

### OpenAPI 认证保护的路径

**精确匹配**：`/v3/chat`, `/v3/chat/cancel`, `/v3/chat/retrieve`, `/v3/chat/message/list`, `/v1/conversations`, `/v1/conversation/create`, `/v1/conversation/message/list`, `/v1/conversation/retrieve`, `/v1/files/upload`, `/v1/workflow/run`, `/v1/workflow/stream_run`, `/v1/workflow/stream_resume`, `/v1/workflow/get_run_history`, `/v1/workflow/conversation/create`, `/v1/bot/get_online_info`, `/v1/workflows/chat`, `/v1/datasets`, `/open_api/knowledge/document/*`

**正则匹配**：`^/v1/conversations/[0-9]+$`, `^/v1/conversations/[0-9]+/clear$`, `^/v1/bots/[0-9]+$`, `^/v1/workflows/[0-9]+$`, `^/v1/apps/[0-9]+$`, `^/v1/datasets/[0-9]+$`, `^/v1/datasets/[0-9]+/images$`, `^/v1/datasets/[0-9]+/process$`

---

## 三、对外接口清单（按模块）

### 3.1 管理后台 (Admin)

> 认证：Session + AdminEmails 白名单  
> 路径前缀：`/api/admin/config`

| 方法 | 路径 | Handler | 功能说明 | 服务调用 |
|------|------|---------|----------|----------|
| GET | `/api/admin/config/basic/get` | GetBasicConfiguration | 获取基础配置 | `bizConf.Base().GetBaseConfig()` |
| POST | `/api/admin/config/basic/save` | SaveBasicConfiguration | 保存基础配置 | `bizConf.Base().SaveBaseConfig()` |
| GET | `/api/admin/config/knowledge/get` | GetKnowledgeConfig | 获取知识库配置 | `bizConf.Knowledge().GetKnowledgeConfig()` |
| POST | `/api/admin/config/knowledge/save` | UpdateKnowledgeConfig | 更新知识库配置 | `bizConf.Knowledge().SaveKnowledgeConfig()` |
| GET | `/api/admin/config/model/list` | GetModelList | 获取模型列表 | `bizConf.ModelConf().GetProviderModelList()` |
| POST | `/api/admin/config/model/create` | CreateModel | 创建模型（含测试验证） | `bizConf.ModelConf().CreateModel()` + modelbuilder |
| POST | `/api/admin/config/model/delete` | DeleteModel | 删除模型 | `bizConf.ModelConf().DeleteModel()` |

---

### 3.2 用户认证 (Passport)

> 认证：登录/注册免认证，其余需 Session  
> 路径前缀：`/api/passport`

| 方法 | 路径 | Handler | 功能说明 | 请求参数 |
|------|------|---------|----------|----------|
| POST | `/api/passport/web/email/register/v2/` | PassportWebEmailRegisterV2Post | 邮箱注册 | Body: email, password 等 |
| POST | `/api/passport/web/email/login/` | PassportWebEmailLoginPost | 邮箱登录 | Body: email, password |
| GET | `/api/passport/web/logout/` | PassportWebLogoutGet | 登出 | — |
| GET | `/api/passport/web/email/password/reset/` | PassportWebEmailPasswordResetGet | 密码重置 | Query: email 等 |
| POST | `/api/passport/account/info/v2/` | PassportAccountInfoV2 | 获取账号信息 | — |

---

### 3.3 用户管理 (User)

> 认证：Session

| 方法 | 路径 | Handler | 功能说明 | 服务调用 |
|------|------|---------|----------|----------|
| POST | `/api/user/update_profile` | UserUpdateProfile | 更新用户资料 | `user.UserApplicationSVC.UserUpdateProfile()` |
| POST | `/api/user/update_profile_check` | UpdateUserProfileCheck | 更新资料预检查 | `user.UserApplicationSVC.UpdateUserProfileCheck()` |
| POST | `/api/web/user/update/upload_avatar/` | UserUpdateAvatar | 上传头像 | `user.UserApplicationSVC.UserUpdateAvatar()` |

---

### 3.4 Agent/Bot 管理 (DraftBot)

> 认证：Session  
> 路径前缀：`/api/draftbot`

| 方法 | 路径 | Handler | 功能说明 | 服务调用 |
|------|------|---------|----------|----------|
| POST | `/api/draftbot/create` | DraftBotCreate | 创建草稿 Agent | `singleagent.SingleAgentSVC.CreateSingleAgentDraft()` |
| POST | `/api/draftbot/delete` | DeleteDraftBot | 删除草稿 Agent | `singleagent.SingleAgentSVC.DeleteAgentDraft()` |
| POST | `/api/draftbot/duplicate` | DuplicateDraftBot | 复制草稿 Agent | `singleagent.SingleAgentSVC.DuplicateDraftBot()` |
| POST | `/api/draftbot/get_display_info` | GetDraftBotDisplayInfo | 获取 Agent 展示信息 | `singleagent.SingleAgentSVC.GetAgentDraftDisplayInfo()` |
| POST | `/api/draftbot/update_display_info` | UpdateDraftBotDisplayInfo | 更新 Agent 展示信息 | `singleagent.SingleAgentSVC.UpdateAgentDraftDisplayInfo()` |
| POST | `/api/draftbot/publish` | PublishDraftBot | 发布 Agent | `singleagent.SingleAgentSVC.PublishAgent()` |
| POST | `/api/draftbot/list_draft_history` | ListDraftBotHistory | 查看发布历史 | `singleagent.SingleAgentSVC.ListAgentPublishHistory()` |
| POST | `/api/draftbot/commit_check` | CheckDraftBotCommit | 发布预检查 | (返回空) |
| POST | `/api/draftbot/publish/connector/list` | PublishConnectorList | 获取发布渠道列表 | `singleagent.SingleAgentSVC.GetPublishConnectorList()` |
| POST | `/api/bot/upload_file` | UploadFile | 上传 Bot 文件 | `upload.SVC.UploadFile()` |
| POST | `/api/bot/get_type_list` | GetTypeList | 获取模型类型列表 | `modelmgr.ModelmgrApplicationSVC.GetModelList()` |

---

### 3.5 对话 (Conversation)

> 认证：Session  
> 路径前缀：`/api/conversation`

| 方法 | 路径 | Handler | 功能说明 | 服务调用 |
|------|------|---------|----------|----------|
| POST | `/api/conversation/chat` | AgentRun | Agent 对话 (SSE 流式) | `conversation.ConversationSVC.Run()` |
| POST | `/api/conversation/create_section` | ClearConversationCtx | 创建新分段 (清除上下文) | `conversation.ConversationSVC.CreateSection()` |
| POST | `/api/conversation/clear_message` | ClearConversationHistory | 清除对话历史 | `conversation.ConversationSVC.ClearHistory()` |
| POST | `/api/conversation/break_message` | BreakMessage | 中断消息生成 | `conversation.ConversationSVC.BreakMessage()` |
| POST | `/api/conversation/delete_message` | DeleteMessage | 删除消息 | `conversation.ConversationSVC.DeleteMessage()` |
| POST | `/api/conversation/get_message_list` | GetMessageList | 获取消息列表 | `conversation.ConversationSVC.GetMessageList()` |

---

### 3.6 消息 (Message) — 见对话模块，v1/v3 消息接口见 Open API 部分

---

### 3.7 知识库 (Knowledge)

> 认证：Session  
> 路径前缀：`/api/knowledge`

**数据集 (Dataset)**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/knowledge/create` | CreateDataset | 创建知识库 |
| POST | `/api/knowledge/detail` | DatasetDetail | 知识库详情 |
| POST | `/api/knowledge/list` | ListDataset | 知识库列表 |
| POST | `/api/knowledge/delete` | DeleteDataset | 删除知识库 |
| POST | `/api/knowledge/update` | UpdateDataset | 更新知识库 |
| POST | `/api/knowledge/icon/get` | GetIconForDataset | 获取知识库图标 |

**文档 (Document)**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/knowledge/document/create` | CreateDocument | 创建文档 |
| POST | `/api/knowledge/document/list` | ListDocument | 文档列表 |
| POST | `/api/knowledge/document/delete` | DeleteDocument | 删除文档 |
| POST | `/api/knowledge/document/update` | UpdateDocument | 更新文档 |
| POST | `/api/knowledge/document/progress/get` | GetDocumentProgress | 获取文档处理进度 |
| POST | `/api/knowledge/document/resegment` | Resegment | 重新分段 |

**图片 (Photo)**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/knowledge/photo/list` | ListPhoto | 图片列表 |
| POST | `/api/knowledge/photo/detail` | PhotoDetail | 图片详情 |
| POST | `/api/knowledge/photo/caption` | UpdatePhotoCaption | 更新图片描述 |
| POST | `/api/knowledge/photo/extract_caption` | ExtractPhotoCaption | AI 提取图片描述 |

**片段 (Slice)**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/knowledge/slice/create` | CreateSlice | 创建片段 |
| POST | `/api/knowledge/slice/list` | ListSlice | 片段列表 |
| POST | `/api/knowledge/slice/update` | UpdateSlice | 更新片段 |
| POST | `/api/knowledge/slice/delete` | DeleteSlice | 删除片段 |

**审核 (Review)**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/knowledge/review/create` | CreateDocumentReview | 创建文档审核 |
| POST | `/api/knowledge/review/mget` | MGetDocumentReview | 批量获取审核 |
| POST | `/api/knowledge/review/save` | SaveDocumentReview | 保存审核 |

**Schema**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/knowledge/table_schema/get` | GetTableSchema | 获取表 Schema |
| POST | `/api/knowledge/table_schema/validate` | ValidateTableSchema | 校验表 Schema |

---

### 3.8 数据库/变量 (Memory/Database)

> 认证：Session  
> 路径前缀：`/api/memory`

**数据库**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/memory/database/list` | ListDatabase | 数据库列表 |
| POST | `/api/memory/database/get_by_id` | GetDatabaseByID | 获取数据库 |
| POST | `/api/memory/database/add` | AddDatabase | 添加数据库 |
| POST | `/api/memory/database/update` | UpdateDatabase | 更新数据库 |
| POST | `/api/memory/database/delete` | DeleteDatabase | 删除数据库 |
| POST | `/api/memory/database/bind_to_bot` | BindDatabase | 绑定数据库到 Bot |
| POST | `/api/memory/database/unbind_to_bot` | UnBindDatabase | 解绑数据库 |
| POST | `/api/memory/database/list_records` | ListDatabaseRecords | 查询数据库记录 |
| POST | `/api/memory/database/update_records` | UpdateDatabaseRecords | 更新数据库记录 |
| POST | `/api/memory/database/get_online_database_id` | GetOnlineDatabaseId | 获取在线数据库 ID |
| POST | `/api/memory/database/get_template` | GetDatabaseTemplate | 获取数据库模板 |
| POST | `/api/memory/database/get_connector_name` | GetConnectorName | 获取连接器名称 |
| POST | `/api/memory/database/table/list_new` | GetBotDatabase | 获取 Bot 数据库表 |
| POST | `/api/memory/database/table/reset` | ResetBotTable | 重置 Bot 表 |
| POST | `/api/memory/database/update_bot_switch` | UpdateDatabaseBotSwitch | 更新数据库开关 |
| POST | `/api/memory/table_schema/get` | GetDatabaseTableSchema | 获取表 Schema |
| POST | `/api/memory/table_schema/validate` | ValidateDatabaseTableSchema | 校验表 Schema |
| POST | `/api/memory/table_file/submit` | SubmitDatabaseInsertTask | 提交数据导入任务 |
| POST | `/api/memory/table_file/get_progress` | DatabaseFileProgressData | 获取导入进度 |

**变量 (Variable / KV Memory)**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| GET | `/api/memory/sys_variable_conf` | GetSysVariableConf | 获取系统变量配置 |
| GET | `/api/memory/project/variable/meta_list` | GetProjectVariableList | 获取项目变量列表 |
| POST | `/api/memory/project/variable/meta_update` | UpdateProjectVariable | 更新项目变量 |
| POST | `/api/memory/variable/upsert` | SetKvMemory | 设置 KV 变量 |
| POST | `/api/memory/variable/get_meta` | GetMemoryVariableMeta | 获取变量元数据 |
| POST | `/api/memory/variable/get` | GetPlayGroundMemory | 获取 Playground 内存 |
| POST | `/api/memory/variable/delete` | DelProfileMemory | 删除变量 |
| GET | `/api/memory/doc_table_info` | GetDocumentTableInfo | 获取文档表信息 |
| GET | `/api/memory/table_mode_config` | GetModeConfig | 获取模式配置 |

---

### 3.9 工作流 (Workflow)

> 认证：Session  
> 路径前缀：`/api/workflow_api`

**CRUD & 管理**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/workflow_api/create` | CreateWorkflow | 创建工作流 |
| POST | `/api/workflow_api/canvas` | GetCanvasInfo | 获取画布信息 |
| POST | `/api/workflow_api/save` | SaveWorkflow | 保存工作流 |
| POST | `/api/workflow_api/update_meta` | UpdateWorkflowMeta | 更新工作流元数据 |
| POST | `/api/workflow_api/delete` | DeleteWorkflow | 删除工作流 |
| POST | `/api/workflow_api/batch_delete` | BatchDeleteWorkflow | 批量删除 |
| POST | `/api/workflow_api/delete_strategy` | GetDeleteStrategy | 获取删除策略 |
| POST | `/api/workflow_api/copy` | CopyWorkflow | 复制工作流 |
| POST | `/api/workflow_api/copy_wk_template` | CopyWkTemplateApi | 复制工作流模板 |
| POST | `/api/workflow_api/workflow_list` | GetWorkFlowList | 工作流列表 |
| POST | `/api/workflow_api/workflow_detail` | GetWorkflowDetail | 工作流详情 |
| POST | `/api/workflow_api/workflow_detail_info` | GetWorkflowDetailInfo | 工作流详情信息 |
| POST | `/api/workflow_api/workflow_references` | GetWorkflowReferences | 工作流引用关系 |
| POST | `/api/workflow_api/history_schema` | GetHistorySchema | 获取历史 Schema |
| POST | `/api/workflow_api/validate_tree` | ValidateTree | 校验工作流树 |

**发布**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/workflow_api/publish` | PublishWorkflow | 发布工作流 |
| POST | `/api/workflow_api/released_workflows` | GetReleasedWorkflows | 已发布工作流列表 |
| POST | `/api/workflow_api/list_publish_workflow` | ListPublishWorkflow | 发布记录列表 |

**运行 & 调试**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/workflow_api/test_run` | WorkFlowTestRun | 测试运行 |
| POST | `/api/workflow_api/test_resume` | WorkFlowTestResume | 测试恢复 |
| POST | `/api/workflow_api/cancel` | CancelWorkFlow | 取消运行 |
| GET | `/api/workflow_api/get_process` | GetWorkFlowProcess | 获取运行进度 |
| GET | `/api/workflow_api/get_node_execute_history` | GetNodeExecuteHistory | 获取节点执行历史 |
| POST | `/api/workflow_api/nodeDebug` | WorkflowNodeDebugV2 | 节点调试 |

**节点**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/workflow_api/node_type` | QueryWorkflowNodeTypes | 查询节点类型 |
| POST | `/api/workflow_api/node_template_list` | NodeTemplateList | 节点模板列表 |
| POST | `/api/workflow_api/node_panel_search` | NodePanelSearch | 节点面板搜索 |
| POST | `/api/workflow_api/llm_fc_setting_detail` | GetLLMNodeFCSettingDetail | LLM 函数调用设置详情 |
| POST | `/api/workflow_api/llm_fc_setting_merged` | GetLLMNodeFCSettingsMerged | LLM 函数调用合并设置 |
| POST | `/api/workflow_api/example_workflow_list` | GetExampleWorkFlowList | 示例工作流列表 |

**追踪 (Trace)**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/workflow_api/get_trace` | GetTraceSDK | 获取追踪数据 |
| POST | `/api/workflow_api/list_spans` | ListRootSpans | 列出根 Span |

**ChatFlow 角色**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| GET | `/api/workflow_api/chat_flow_role/get` | GetChatFlowRole | 获取角色 |
| POST | `/api/workflow_api/chat_flow_role/create` | CreateChatFlowRole | 创建角色 |
| POST | `/api/workflow_api/chat_flow_role/delete` | DeleteChatFlowRole | 删除角色 |

**项目对话定义**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/workflow_api/project_conversation/create` | CreateProjectConversationDef | 创建项目对话定义 |
| POST | `/api/workflow_api/project_conversation/update` | UpdateProjectConversationDef | 更新项目对话定义 |
| POST | `/api/workflow_api/project_conversation/delete` | DeleteProjectConversationDef | 删除项目对话定义 |
| GET | `/api/workflow_api/project_conversation/list` | ListProjectConversationDef | 项目对话定义列表 |

**其他**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| GET | `/api/workflow_api/apiDetail` | GetApiDetail | 获取 API 详情 |
| POST | `/api/workflow_api/sign_image_url` | SignImageURL | 签名图片 URL |
| POST | `/api/workflow_api/upload/auth_token` | GetWorkflowUploadAuthToken | 获取上传凭证 |

---

### 3.10 插件 (Plugin)

> 认证：Session  
> 路径前缀：`/api/plugin_api`, `/api/plugin`

**注册 & 管理**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/plugin_api/register` | RegisterPlugin | 注册插件 |
| POST | `/api/plugin_api/register_plugin_meta` | RegisterPluginMeta | 注册插件元数据 |
| POST | `/api/plugin_api/update` | UpdatePlugin | 更新插件 |
| POST | `/api/plugin_api/update_plugin_meta` | UpdatePluginMeta | 更新插件元数据 |
| POST | `/api/plugin_api/del_plugin` | DelPlugin | 删除插件 |
| POST | `/api/plugin_api/get_plugin_info` | GetPluginInfo | 获取插件信息 |
| POST | `/api/plugin_api/get_dev_plugin_list` | GetDevPluginList | 开发插件列表 |
| POST | `/api/plugin_api/get_playground_plugin_list` | GetPlaygroundPluginList | Playground 插件列表 |
| POST | `/api/plugin_api/get_plugin_next_version` | GetPluginNextVersion | 获取下一版本号 |
| POST | `/api/plugin_api/publish_plugin` | PublishPlugin | 发布插件 |

**API 管理**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/plugin_api/create_api` | CreateAPI | 创建 API |
| POST | `/api/plugin_api/update_api` | UpdateAPI | 更新 API |
| POST | `/api/plugin_api/delete_api` | DeleteAPI | 删除 API |
| POST | `/api/plugin_api/batch_create_api` | BatchCreateAPI | 批量创建 API |
| POST | `/api/plugin_api/get_plugin_apis` | GetPluginAPIs | 获取插件 API 列表 |
| POST | `/api/plugin_api/get_updated_apis` | GetUpdatedAPIs | 获取已更新 API |
| POST | `/api/plugin_api/debug_api` | DebugAPI | 调试 API |
| POST | `/api/plugin_api/convert_to_openapi` | Convert2OpenAPI | 转换为 OpenAPI 格式 |

**OAuth & 权限**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/plugin/get_oauth_schema` | GetOAuthSchema | 获取 OAuth Schema |
| POST | `/api/plugin_api/get_oauth_schema` | GetOAuthSchemaAPI | 获取 OAuth Schema (API) |
| POST | `/api/plugin_api/get_oauth_status` | GetOAuthStatus | 获取 OAuth 状态 |
| POST | `/api/plugin_api/get_queried_oauth_plugins` | GetQueriedOAuthPluginList | 查询 OAuth 插件列表 |
| POST | `/api/plugin_api/revoke_auth_token` | RevokeAuthToken | 撤销 Auth Token |
| POST | `/api/plugin_api/get_user_authority` | GetUserAuthority | 获取用户权限 |

**编辑锁**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/plugin_api/check_and_lock_plugin_edit` | CheckAndLockPluginEdit | 检查并锁定编辑 |
| POST | `/api/plugin_api/unlock_plugin_edit` | UnlockPluginEdit | 解锁编辑 |

**Bot 默认参数**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/plugin_api/get_bot_default_params` | GetBotDefaultParams | 获取 Bot 默认参数 |
| POST | `/api/plugin_api/update_bot_default_params` | UpdateBotDefaultParams | 更新 Bot 默认参数 |

**资源**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/plugin_api/library_resource_list` | LibraryResourceList | 资源库列表 |
| POST | `/api/plugin_api/project_resource_list` | ProjectResourceList | 项目资源列表 |
| POST | `/api/plugin_api/resource_copy_dispatch` | ResourceCopyDispatch | 资源复制派发 |
| POST | `/api/plugin_api/resource_copy_detail` | ResourceCopyDetail | 资源复制详情 |
| POST | `/api/plugin_api/resource_copy_retry` | ResourceCopyRetry | 资源复制重试 |
| POST | `/api/plugin_api/resource_copy_cancel` | ResourceCopyCancel | 资源复制取消 |

---

### 3.11 应用/项目 (Intelligence/App)

> 认证：Session  
> 路径前缀：`/api/intelligence_api`

**草稿项目**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/intelligence_api/draft_project/create` | DraftProjectCreate | 创建草稿项目 |
| POST | `/api/intelligence_api/draft_project/update` | DraftProjectUpdate | 更新草稿项目 |
| POST | `/api/intelligence_api/draft_project/delete` | DraftProjectDelete | 删除草稿项目 |
| POST | `/api/intelligence_api/draft_project/copy` | DraftProjectCopy | 复制项目 |
| POST | `/api/intelligence_api/draft_project/inner_task_list` | DraftProjectInnerTaskList | 项目内部任务列表 |

**搜索**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/intelligence_api/search/get_draft_intelligence_list` | GetDraftIntelligenceList | 草稿智能体列表 |
| POST | `/api/intelligence_api/search/get_draft_intelligence_info` | GetDraftIntelligenceInfo | 草稿智能体信息 |
| POST | `/api/intelligence_api/search/get_recently_edit_intelligence` | GetUserRecentlyEditIntelligence | 最近编辑列表 |

**发布**

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/intelligence_api/publish/publish_project` | PublishProject | 发布项目 |
| POST | `/api/intelligence_api/publish/check_version_number` | CheckProjectVersionNumber | 检查版本号 |
| POST | `/api/intelligence_api/publish/connector_list` | ProjectPublishConnectorList | 发布渠道列表 |
| POST | `/api/intelligence_api/publish/get_published_connector` | GetProjectPublishedConnector | 获取已发布渠道 |
| POST | `/api/intelligence_api/publish/publish_record_list` | GetPublishRecordList | 发布记录列表 |
| POST | `/api/intelligence_api/publish/publish_record_detail` | GetPublishRecordDetail | 发布记录详情 |

---

### 3.12 市场 (Marketplace)

> 认证：Session  
> 路径前缀：`/api/marketplace/product`

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| GET | `/api/marketplace/product/list` | PublicGetProductList | 产品列表 |
| GET | `/api/marketplace/product/detail` | PublicGetProductDetail | 产品详情 |
| GET | `/api/marketplace/product/search` | PublicSearchProduct | 搜索产品 |
| GET | `/api/marketplace/product/search/suggest` | PublicSearchSuggest | 搜索建议 |
| GET | `/api/marketplace/product/category/list` | PublicGetProductCategoryList | 产品分类列表 |
| GET | `/api/marketplace/product/call_info` | PublicGetProductCallInfo | 产品调用信息 |
| GET | `/api/marketplace/product/config` | PublicGetMarketPluginConfig | 市场插件配置 |
| POST | `/api/marketplace/product/duplicate` | PublicDuplicateProduct | 复制产品 |
| POST | `/api/marketplace/product/favorite` | PublicFavoriteProduct | 收藏产品 |
| GET | `/api/marketplace/product/favorite/list.v2` | PublicGetUserFavoriteListV2 | 用户收藏列表 |

---

### 3.13 Playground

> 认证：Session  
> 路径前缀：`/api/playground`, `/api/playground_api`

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/playground/get_onboarding` | GetOnboarding | 获取引导信息 |
| POST | `/api/playground/upload/auth_token` | GetUploadAuthToken | 获取上传凭证 |
| POST | `/api/playground_api/draftbot/get_draft_bot_info` | GetDraftBotInfoAgw | 获取草稿 Bot 信息 |
| POST | `/api/playground_api/draftbot/update_draft_bot_info` | UpdateDraftBotInfoAgw | 更新草稿 Bot 信息 |
| POST | `/api/playground_api/get_imagex_url` | GetImagexShortUrl | 获取 ImageX 短链 |
| POST | `/api/playground_api/mget_user_info` | MGetUserBasicInfo | 批量获取用户信息 |
| POST | `/api/playground_api/get_official_prompt_list` | GetOfficialPromptResourceList | 官方 Prompt 列表 |
| GET | `/api/playground_api/get_prompt_resource_info` | GetPromptResourceInfo | Prompt 资源信息 |
| POST | `/api/playground_api/upsert_prompt_resource` | UpsertPromptResource | 创建/更新 Prompt |
| POST | `/api/playground_api/delete_prompt_resource` | DeletePromptResource | 删除 Prompt |
| POST | `/api/playground_api/create_update_shortcut_command` | CreateUpdateShortcutCommand | 创建/更新快捷命令 |
| POST | `/api/playground_api/get_file_list` | GetFileUrls | 获取文件列表 |
| POST | `/api/playground_api/report_user_behavior` | ReportUserBehavior | 上报用户行为 |
| POST | `/api/playground_api/operate/get_bot_popup_info` | GetBotPopupInfo | Bot 弹窗信息 |
| POST | `/api/playground_api/operate/update_bot_popup_info` | UpdateBotPopupInfo | 更新 Bot 弹窗 |
| POST | `/api/playground_api/space/list` | GetSpaceListV2 | Space 列表 |
| POST | `/api/developer/get_icon` | GetIcon | 获取图标 |

---

### 3.14 上传 (Upload)

> 认证：Session  
> 路径前缀：`/api/common/upload`

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/common/upload/*tos_uri` | CommonUpload | 通用文件上传 (支持路径通配) |
| GET | `/api/common/upload/apply_upload_action` | ApplyUploadAction | 申请上传动作 (GET) |
| POST | `/api/common/upload/apply_upload_action` | ApplyUploadAction | 申请上传动作 (POST) |

---

### 3.15 权限/PAT (Permission)

> 认证：Session  
> 路径前缀：`/api/permission_api/pat`

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/api/permission_api/pat/create_personal_access_token_and_permission` | CreatePersonalAccessTokenAndPermission | 创建 PAT |
| GET | `/api/permission_api/pat/get_personal_access_token_and_permission` | GetPersonalAccessTokenAndPermission | 获取 PAT 详情 |
| POST | `/api/permission_api/pat/update_personal_access_token_and_permission` | UpdatePersonalAccessTokenAndPermission | 更新 PAT |
| POST | `/api/permission_api/pat/delete_personal_access_token_and_permission` | DeletePersonalAccessTokenAndPermission | 删除 PAT |
| GET | `/api/permission_api/pat/list_personal_access_tokens` | ListPersonalAccessTokens | PAT 列表 |

---

### 3.16 OAuth

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| GET | `/api/oauth/authorization_code` | OauthAuthorizationCode | OAuth 授权码获取 |
| POST | `/api/permission_api/coze_web_app/impersonate_coze_user` | ImpersonateCozeUser | 模拟 Coze 用户 |

---

## 四、Open API（v1/v3 外部集成接口）

> 认证：Bearer Token (PAT)  
> 用途：供外部系统通过 SDK/API 集成

### 4.1 Chat v3

| 方法 | 路径 | Handler | 功能说明 | 响应方式 |
|------|------|---------|----------|----------|
| POST | `/v3/chat` | ChatV3 | 发起对话 | SSE 流式 / 同步 |
| POST | `/v3/chat/cancel` | CancelChatApi | 取消对话 | JSON |
| GET | `/v3/chat/retrieve` | RetrieveChatOpen | 获取对话运行记录 | JSON |
| GET | `/v3/chat/message/list` | ListChatMessageApi | 获取对话消息列表 | JSON |

### 4.2 Conversation v1

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/v1/conversation/create` | CreateConversation | 创建会话 |
| GET | `/v1/conversation/retrieve` | RetrieveConversationApi | 获取会话 |
| POST | `/v1/conversation/message/list` | GetApiMessageList | 消息列表 |
| GET | `/v1/conversations` | ListConversationsApi | 会话列表 |
| PUT | `/v1/conversations/:conversation_id` | UpdateConversationApi | 更新会话 |
| DELETE | `/v1/conversations/:conversation_id` | DeleteConversationApi | 删除会话 |
| POST | `/v1/conversations/:conversation_id/clear` | ClearConversationApi | 清除会话 |

### 4.3 Workflow v1

| 方法 | 路径 | Handler | 功能说明 | 响应方式 |
|------|------|---------|----------|----------|
| POST | `/v1/workflow/run` | OpenAPIRunFlow | 运行工作流 | JSON |
| POST | `/v1/workflow/stream_run` | OpenAPIStreamRunFlow | 流式运行工作流 | SSE |
| POST | `/v1/workflow/stream_resume` | OpenAPIStreamResumeFlow | 流式恢复工作流 | SSE |
| GET | `/v1/workflow/get_run_history` | OpenAPIGetWorkflowRunHistory | 获取运行历史 | JSON |
| POST | `/v1/workflow/conversation/create` | OpenAPICreateConversation | 创建工作流会话 | JSON |
| POST | `/v1/workflows/chat` | OpenAPIChatFlowRun | ChatFlow 运行 | SSE |
| GET | `/v1/workflows/:workflow_id` | OpenAPIGetWorkflowInfo | 获取工作流信息 | JSON |

### 4.4 Knowledge/Dataset v1

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/v1/datasets` | CreateDatasetOpenAPI | 创建数据集 |
| GET | `/v1/datasets` | ListDatasetOpenAPI | 数据集列表 |
| PUT | `/v1/datasets/:dataset_id` | UpdateDatasetOpenAPI | 更新数据集 |
| DELETE | `/v1/datasets/:dataset_id` | DeleteDatasetOpenAPI | 删除数据集 |
| GET | `/v1/datasets/:dataset_id/images` | ListPhotoDocumentOpenAPI | 图片文档列表 |
| POST | `/v1/datasets/:dataset_id/process` | GetDocumentProgressOpenAPI | 获取文档处理进度 |

### 4.5 Bot v1

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| GET | `/v1/bot/get_online_info` | GetBotOnlineInfo | 获取 Bot 在线信息 |
| GET | `/v1/bots/:bot_id` | OpenGetBotInfo | 获取 Bot 信息 |

### 4.6 App v1

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| GET | `/v1/apps/:app_id` | GetOnlineAppData | 获取在线应用数据 |

### 4.7 File v1

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/v1/files/upload` | UploadFileOpen | 上传文件 |

### 4.8 Knowledge Document (open_api)

| 方法 | 路径 | Handler | 功能说明 |
|------|------|---------|----------|
| POST | `/open_api/knowledge/document/create` | CreateDocumentOpenAPI | 创建文档 |
| POST | `/open_api/knowledge/document/list` | ListDocumentOpenAPI | 文档列表 |
| POST | `/open_api/knowledge/document/delete` | DeleteDocumentOpenAPI | 删除文档 |
| POST | `/open_api/knowledge/document/update` | UpdateDocumentOpenAPI | 更新文档 |

---

## 五、文件上传下载接口

| 方法 | 路径 | 认证 | 功能说明 | 参数 |
|------|------|------|----------|------|
| POST | `/api/common/upload/*tos_uri` | Session | 通用文件上传 | Body + path (tos_uri) |
| GET/POST | `/api/common/upload/apply_upload_action` | Session | 申请/提交上传动作 | Query/Body |
| POST | `/api/bot/upload_file` | Session | Bot 文件上传 | Body: UploadFileRequest |
| POST | `/api/web/user/update/upload_avatar/` | Session | 用户头像上传 | Form: avatar file |
| POST | `/v1/files/upload` | Bearer Token | 文件上传 (Open API) | Body: UploadFileOpenRequest |
| POST | `/api/playground/upload/auth_token` | Session | 获取上传凭证 | Body |
| POST | `/api/workflow_api/upload/auth_token` | Session | 工作流上传凭证 | Body |
| POST | `/api/workflow_api/sign_image_url` | Session | 签名图片 URL | Body |

> 注意：Nginx 层 `/local_storage/` 路径直接代理到 MinIO，用于文件下载。

---

## 六、SSE 流式接口

以下接口使用 Server-Sent Events (SSE) 协议进行流式响应：

| 路径 | 功能 | 认证 |
|------|------|------|
| `POST /api/conversation/chat` | 内部 Agent 对话 | Session |
| `POST /v3/chat` | Chat v3 (可 stream/sync) | Bearer Token |
| `POST /v1/workflow/stream_run` | 工作流流式运行 | Bearer Token |
| `POST /v1/workflow/stream_resume` | 工作流流式恢复 | Bearer Token |
| `POST /v1/workflows/chat` | ChatFlow 运行 | Bearer Token |

> Nginx 配置 SSE 超时为 600s。  
> 使用 `hertz-contrib/sse` 库实现。

---

## 七、内部服务调用清单

### Application Services (应用服务层)

| 服务 | 包路径 | 职责 |
|------|--------|------|
| `ConversationSVC` | `application/conversation` | 会话管理、Agent 运行 |
| `ConversationOpenAPISVC` | `application/conversation` | 开放 API 对话 |
| `OpenapiMessageSVC` | `application/conversation` | 开放 API 消息 |
| `SingleAgentSVC` | `application/singleagent` | Agent 草稿/发布/信息管理 |
| `KnowledgeSVC` | `application/knowledge` | 知识库 CRUD、文档处理 |
| `VariableApplicationSVC` | `application/memory` | 变量/记忆管理 |
| `DatabaseApplicationSVC` | `application/memory` | 数据库管理 |
| `SVC (workflow)` | `application/workflow` | 工作流全生命周期 |
| `PluginApplicationSVC` | `application/plugin` | 插件管理 |
| `UserApplicationSVC` | `application/user` | 用户认证/资料 |
| `APPApplicationSVC` | `application/app` | 应用/项目管理 |
| `SVC (upload)` | `application/upload` | 文件上传 |
| `SearchSVC` | `application/search` | 搜索 |
| `OpenAuthApplication` | `application/openauth` | PAT/OAuth 管理 |
| `PromptSVC` | `application/prompt` | Prompt 资源管理 |
| `ShortcutCmdSVC` | `application/shortcutcmd` | 快捷命令 |
| `ModelmgrApplicationSVC` | `application/modelmgr` | 模型管理 |

### 外部系统调用

| 外部系统 | 调用位置 | 调用目的 |
|----------|----------|----------|
| **MySQL** | `infra/orm`, `infra/rdb` | 业务数据持久化 (~50 张表) |
| **Redis** | `infra/cache` | Session 缓存、业务缓存 |
| **Elasticsearch** | `infra/es` | 知识库全文检索、智能搜索 |
| **Milvus** | `infra/embedding` | 向量相似度检索 (RAG) |
| **MinIO/TOS/S3** | `infra/storage` | 文件/图片对象存储 |
| **NSQ/Kafka/RocketMQ** | `infra/eventbus` | 异步事件处理、文档处理队列 |
| **LLM (Ark/OpenAI/etc.)** | `cloudwego/eino-ext` | 大模型推理、对话生成 |
| **Embedding Provider** | `infra/embedding` | 文本向量化 |
| **Code Runner (Python/Deno)** | `infra/coderunner` | 工作流代码节点执行 |
| **Document Parser** | `infra/document` | PDF/DOCX 文档解析 |
| **OCR Service** | `infra/document` | 图片 OCR |
| **Rerank Service** | `infra/document` | 搜索结果重排序 |
| **VolcEngine ImageX** | `infra/imagex` | 图片处理/CDN |

---

## 八、消息队列事件

> 消息队列类型由 `COZE_MQ_TYPE` 环境变量控制，支持 NSQ/Kafka/RocketMQ/Pulsar/NATS。

消息队列主要用于以下场景（基于 `infra/eventbus` 包）：

| 场景 | 说明 |
|------|------|
| 文档处理 | 知识库文档上传后异步解析、分段、向量化 |
| 数据导入 | 数据库文件导入任务异步执行 |
| 资源复制 | 跨项目资源复制的异步派发 |
| 事件通知 | 发布、状态变更等事件的异步广播 |

---

## 九、接口统计

| 模块 | IDL Service | 接口数 |
|------|-------------|--------|
| Admin (Config) | ConfigService | 7 |
| Passport | PassportService | 7 |
| User | (PassportService) | 3 |
| DraftBot / Agent | DeveloperApiService, BotOpenApiService | 16 |
| Conversation | ConversationService, AgentRunService | 12 |
| Message | MessageService | 5 |
| Knowledge | DatasetService | 36 |
| Memory / Database | DatabaseService, MemoryService | 28 |
| Workflow | WorkflowService | 52 |
| Plugin | PluginDevelopService | 27 |
| Resource | ResourceService | 6 |
| Intelligence / App | IntelligenceService | 15 |
| Marketplace | PublicProductService | 10 |
| Playground | PlaygroundService | 17 |
| Upload | UploadService | 3 |
| Permission / PAT | OpenAPIAuthService | 7 |
| **合计** | **18 Services** | **~251** |

---

## 附录：Thrift IDL 文件清单

```
idl/
├── api.thrift                              # 聚合入口
├── base.thrift                             # 基础类型
├── admin/config.thrift                     # 管理后台配置
├── app/
│   ├── bot_common.thrift                   # Bot 公共类型
│   ├── bot_open_api.thrift                 # Bot Open API
│   ├── developer_api.thrift                # 开发者 API
│   ├── intelligence.thrift                 # 智能体/项目
│   ├── project.thrift                      # 项目
│   ├── publish.thrift                      # 发布
│   ├── search.thrift                       # 搜索
│   ├── task.thrift                         # 任务
│   └── common_struct/                      # 公共结构体
├── conversation/
│   ├── agentrun_service.thrift             # Agent 运行
│   ├── common.thrift
│   ├── conversation.thrift
│   ├── conversation_service.thrift         # 会话服务
│   ├── message.thrift
│   ├── message_service.thrift              # 消息服务
│   └── run.thrift
├── data/
│   ├── database/database_svc.thrift        # 数据库服务
│   ├── knowledge/knowledge_svc.thrift      # 知识库服务
│   └── variable/variable_svc.thrift        # 变量服务
├── marketplace/public_api.thrift           # 市场
├── passport/passport.thrift                # 用户认证
├── permission/openapiauth_service.thrift   # 权限
├── playground/playground.thrift            # Playground
├── plugin/plugin_develop.thrift            # 插件开发
├── resource/resource.thrift                # 资源
├── upload/upload.thrift                    # 上传
└── workflow/workflow_svc.thrift            # 工作流
```
