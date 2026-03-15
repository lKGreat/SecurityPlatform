# 主对象定义与冲突证据盘点（SEC-29）

> 目的：仅做证据盘点与并排对比，不在本文件中做术语裁决。  
> 基线：以 `docs/` 文档现状为准。  
> 范围：Tenant / App / Runtime 及其相关对象（Application / Project / DataSource / Space / Workspace / Agent / Workflow / Knowledge / Marketplace）。

---

## 1. 文档来源表

| 文档名 | 章节/标题 | 原始对象名 | 当前定义摘要（转述） | 相关层级 |
|---|---|---|---|---|
| `docs/架构与产品能力总览.md` | 二、架构概览 → 2.3 领域边界 | Apps | Apps 被定义为“租户-应用开通”上下文，侧重租户开通关系。 | 租户 / 应用 |
| `docs/架构与产品能力总览.md` | 二、架构概览 → 2.3 领域边界 | TenantDataSources | 数据源被定义为“租户-应用级 SQL Server 连接”。 | 租户 / 应用 / 运行 |
| `docs/架构与产品能力总览.md` | 二、架构概览 → 2.3 领域边界 | Projects | Project 被定义为“项目域隔离”。 | 应用 / 工作台 |
| `docs/架构与产品能力总览.md` | 二、架构概览 → 2.4 多租户架构 | Tenant / Tenant-App | 明确存在租户-应用订阅（Tenant-App）与项目模式、多数据源扩展。 | 平台 / 租户 / 应用 |
| `docs/架构与产品能力总览.md` | 四、产品功能总览 → 4.4 / 4.5 | Runtime / Workflow | Runtime 出现在审批运行时控制（ApprovalRuntime），Workflow 则是定义、实例与事件。 | 运行 / 发布 |
| `docs/多租户多应用.md` | 二、范围定义（对象与关系） | Tenant | Tenant 为租户隔离边界主体。 | 平台 / 租户 |
| `docs/多租户多应用.md` | 二、范围定义（对象与关系） | Application | Application 为平台业务应用/子系统。 | 应用 |
| `docs/多租户多应用.md` | 二、范围定义（对象与关系） | TenantApplication（Tenant-App） | Tenant-App 是“租户开通应用后的实体关系”，承载该租户使用该应用的配置。 | 租户 / 应用 |
| `docs/多租户多应用.md` | 二、范围定义（对象与关系） | Project | Project 是应用内业务实体，用于权限与数据范围隔离，且明确“不作为数据源维度”。 | 应用 / 工作台 |
| `docs/多租户多应用.md` | 二、范围定义（对象与关系） | DataSource | DataSource 是 SQL Server 连接配置，绑定在 Tenant-App。 | 租户 / 应用 / 运行 |
| `docs/plan-平台控制台与应用数据源.md` | 一、背景与目标 → 1.2 目标 | Platform Console | 登录后先进入平台控制台（顶部导航），用于新建应用/配置数据源/数据迁移。 | 平台 |
| `docs/plan-平台控制台与应用数据源.md` | 一、背景与目标 → 1.2 目标 | App Workspace | 点击应用后进入应用工作台（应用专属侧边栏）。 | 应用 / 工作台 |
| `docs/plan-平台控制台与应用数据源.md` | 二、核心约束 → 2.1 数据源不可变 | DataSourceId | App 创建时绑定 DataSourceId，绑定后不可修改。 | 应用 / 运行 |
| `docs/plan-平台控制台与应用数据源.md` | 四、数据模型 → 4.1 / 4.2 | LowCodeApp / TenantDataSource | LowCodeApp 持有 DataSourceId；TenantDataSource 以 AppId 区分平台级或应用级。 | 平台 / 应用 / 运行 |
| `docs/coze-studio-feature-atlas.md` | 1.1 域2 空间与工作区 | Space / Workspace | Space 被用作工作空间容器，路由以 `/space/:space_id/*` 组织。 | 工作台 |
| `docs/coze-studio-feature-atlas.md` | 1.1 域3/域4 | Agent / Project / App | Agent 与 Project/App 并列为创作资源，且开发列表同时展示 Agent/App/项目。 | 应用 / 工作台 / 发布 |
| `docs/coze-studio-feature-atlas.md` | 1.1 域5/域6/域10 | Workflow / Knowledge / Marketplace | Workflow、Knowledge、Marketplace 为并列能力域，均在 Space 作用域下被浏览/编辑/发布。 | 工作台 / 发布 |
| `docs/coze-studio-project-cognitive-map.md` | C. 启动流程（前端） | SpaceLayout / ProjectIDE / WorkflowPage | 前端路由将 `/space/*` 作为主工作域，`/project-ide/*`、`/work_flow` 分置。 | 工作台 / 运行 |

---

## 2. 冲突证据表

| 对象名 | 冲突类型 | 冲突说明 | 证据来源 A | 证据来源 B |
|---|---|---|---|---|
| Application / App | 同名异义 | 在平台文档中，Application 更接近“可被租户开通的业务应用”；在 Coze 文档中，App/项目又承载创作资源与发布对象语义。 | `docs/多租户多应用.md`（Application=业务应用；Tenant-App 承载配置） | `docs/coze-studio-feature-atlas.md`（“应用/项目(App)”=创建/复制/发布资源） |
| Application / Tenant-App | 边界缺失 | 有的文档把“应用管理”与“租户开通”合并描述，有的文档将其拆成 Application 与 Tenant-App 两层，导致边界未闭合。 | `docs/架构与产品能力总览.md`（Apps=租户-应用开通） | `docs/多租户多应用.md`（Application 与 Tenant-App 分离定义） |
| DataSource | 同名异义 | 在多租户文档中 DataSource 主要是 Tenant-App 级连接；在平台控制台方案里 DataSourceId 变成 App 创建时一次绑定、并强调不可变。 | `docs/多租户多应用.md`（DataSource 绑定 Tenant-App） | `docs/plan-平台控制台与应用数据源.md`（LowCodeApp.DataSourceId 创建后不可改） |
| DataSource 归属层级 | 层级冲突 | 一个文档强调 Tenant-App 必绑主数据源；另一个文档支持 TenantDataSource 的平台级（AppId=null）与应用级并存。 | `docs/多租户多应用.md`（每个 Tenant-App 必绑主数据源） | `docs/plan-平台控制台与应用数据源.md`（TenantDataSource 可平台级/应用级） |
| Project | 同名异义 | 在多租户文档中 Project 是“应用内权限与数据范围实体（非数据源维度）”；在 Coze 文档中 Project 与 App/Agent 作为 IDE 资源对象出现，语义更接近“可创作产物”。 | `docs/多租户多应用.md`（Project 非数据源维度） | `docs/coze-studio-feature-atlas.md`（Project IDE、DraftProjectCreate/Publish） |
| Workspace | 异名同义 | “应用工作台（App Workspace）”与 “Space/Workspace”都表达工作域容器，但命名体系不同，且是否绑定租户/应用未统一。 | `docs/plan-平台控制台与应用数据源.md`（App Workspace） | `docs/coze-studio-feature-atlas.md`（Space / Workspace） |
| Runtime | 边界缺失 | Runtime 在平台文档中通过审批运行时接口与操作出现，但没有形成统一的“运行时对象”定义（与 WorkflowRuntime、AppRuntime 关系不清）。 | `docs/架构与产品能力总览.md`（ApprovalRuntimeController/运行时操作） | `docs/plan-平台控制台与应用数据源.md`（仅定义控制台与工作台、未定义 Runtime 术语） |
| Workflow | 层级冲突 | 平台文档将 Workflow 定义为通用业务能力；Coze 文档中 Workflow 既是 IDE 资源，又有 OpenAPI 运行面向外部调用。 | `docs/架构与产品能力总览.md`（Workflow 定义/实例/事件） | `docs/coze-studio-feature-atlas.md`（Workflow CRUD+发布+OpenAPI 运行） |
| Knowledge | 边界缺失 | Coze 文档将 Knowledge 作为独立资源与 RAG 数据域；平台文档体系中未形成同级核心对象定义，容易造成“资源对象 vs 平台能力”混用。 | `docs/coze-studio-feature-atlas.md`（Knowledge 模块完整生命周期） | `docs/架构与产品能力总览.md`（未出现对 Knowledge 的主对象定义） |
| Marketplace | 层级冲突 | Coze 中 Marketplace 是探索广场与产品分发域；平台文档未给出对应主对象层级，后续若接入将出现发布层归属不清。 | `docs/coze-studio-feature-atlas.md`（Explore / Marketplace） | `docs/架构与产品能力总览.md`（无 Marketplace 对应主对象） |

---

## 3. 收敛候选对象清单

| 对象名 | 为什么必须进入统一词汇表 | 关联对象 | 风险等级 |
|---|---|---|---|
| Tenant | 所有隔离、鉴权、订阅关系都以 Tenant 为根；若定义漂移将直接影响权限模型。 | Tenant-App, User, DataScope | 高 |
| Application（产品目录） | 目前与 App/Project 语义混叠，需锁定“产品目录项”语义。 | Tenant-App, Console, Publish | 高 |
| Tenant-App（租户订阅） | 承载租户开通状态、数据源配置、可用性状态，是平台运营闭环关键对象。 | Tenant, Application, DataSource | 高 |
| App（创作产物） | 在 Coze 语境中是项目型产物，与平台 Application 易冲突，需明确别名或分层。 | Project, Agent, Workflow, Release | 高 |
| Project | 同时承担“权限域”与“IDE 资源”语义，需要明确是否一个对象或两类对象。 | App, Workspace, DataScope | 高 |
| DataSource | 当前存在 Tenant-App 级、应用级、平台级多种说法，需统一归属层级与生命周期。 | Tenant-App, App, Runtime | 高 |
| Workspace / Space / App Workspace | 工作域容器命名不一致，影响路由、权限边界与 UI 信息架构。 | Console, App, Agent, Project | 中 |
| Runtime | 高频使用但缺乏明确对象边界，影响运行态监控、审计与发布回滚定义。 | WorkflowRuntime, ApprovalRuntime, AppRun | 高 |
| Agent | 与 App/Project 的包含关系未固化，需纳入词汇表以防接口命名漂移。 | Workspace, Workflow, Knowledge | 中 |
| Workflow | 同时存在设计态与运行态；且有内部接口与 OpenAPI 双语义。 | Runtime, Project, Agent | 中 |
| Knowledge | 作为资源对象与数据资产对象双重属性，影响权限与存储策略。 | Agent, Workflow, Marketplace | 中 |
| Marketplace | 涉及发布域与分发域，若不收敛会影响发布模型和运营指标口径。 | Publish, Connector, Template | 中 |

---

## 4. 一页结论摘要

### 4.1 当前最严重的 5 个命名/边界问题

1. **Application 语义过载**：在不同文档中同时承担“平台应用目录”“租户订阅对象”“创作项目对象”语义。  
2. **DataSource 归属不统一**：有 Tenant-App 必绑主数据源、App 创建绑定 DataSourceId 不可变、平台级/应用级并存三套口径。  
3. **Project 与 App 关系未闭合**：一处是权限/数据范围隔离实体，一处是 IDE 可发布产物。  
4. **Workspace 命名体系分裂**：App Workspace 与 Space/Workspace 并存，尚未统一到同一分层模型。  
5. **Runtime 高风险缺定义**：运行态在审批/工作流/应用多个域出现，但尚未形成统一主对象与生命周期表述。

### 4.2 若不收敛，对后续任务的影响

- **影响 SEC-31（术语词汇表）**：无法稳定映射“一词一义”，会导致词汇表变成描述性文档而非约束性文档。  
- **影响接口契约收敛**：`Application/App/Project` 命名混用会反复引发 DTO 与路由命名调整。  
- **影响权限与数据隔离设计**：`Tenant-App / DataSource / Workspace` 边界不稳，会放大越权与数据串读风险。  
- **影响发布与运行治理**：`Runtime / Workflow / Marketplace` 层级未定，会导致发布、审计、回滚口径不一致。

---

## 5. 建议给 SEC-31 的直接输入（仅范围，不做裁决）

建议 SEC-31 先以以下对象为第一批词汇表强约束范围（必须定义：唯一名称、同义词、所属层级、生命周期、与其他对象关系）：

- Tenant
- Application（平台应用目录）
- Tenant-App（租户订阅）
- App（创作产物）
- Project
- DataSource
- Workspace（及 Space/App Workspace 映射）
- Runtime
- Agent
- Workflow
- Knowledge
- Marketplace

