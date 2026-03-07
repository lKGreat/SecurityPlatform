# Plan: 平台控制台与应用数据源

> 本文档定义平台控制台布局、应用工作台、应用级数据源、数据共享策略及实体别名的完整需求与设计约束。  
> 更新规则：架构变更或功能完成时同步更新本文档。

---

## 一、背景与目标

### 1.1 背景

当前前端采用单层管理后台布局，登录后直接进入带侧边栏的传统后台，存在以下问题：

- 缺少「平台控制台」概念，应用与平台管理混在一起
- 应用与数据源的关系不清晰
- 基础数据（用户/角色/部门）的跨应用共享与隔离无法配置

### 1.2 目标

- 登录后进入**平台控制台**（纯顶部导航），可新建应用、配置数据源、执行数据迁移
- 点击应用后进入**应用工作台**（应用专属侧边栏）
- 每个应用可绑定**独立数据源**（支持 SqlSugar 多驱动）
- 基础数据支持**应用级共享策略**（继承平台 / 应用独立）
- 支持**实体别名**，不同应用可对同一实体使用不同显示名称

---

## 二、核心约束（必读）

### 2.1 数据源不可变

| 约束 | 说明 |
|------|------|
| **创建后不可更改** | 应用创建时绑定的数据源（`DataSourceId`）一旦确定，**永不允许修改** |
| **原因** | 数据源绑定物理存储位置，应用内所有动态表、表单数据、审批记录已写入该数据源；变更会导致历史数据孤立、schema 缺失、关联失效 |
| **实现** | `LowCodeApp.DataSourceId` 仅构造函数可设，无 `UpdateDataSource` 方法；`PUT /api/v1/apps/{id}` 的 DTO 不含 `DataSourceId` |

### 2.2 可修改的基础数据

| 可修改项 | 说明 |
|----------|------|
| 应用元信息 | Name、Description、Icon、Category |
| 数据共享策略 | UseSharedUsers、UseSharedRoles、UseSharedDepartments |
| 实体别名 | user/role/department 的 SingularAlias、PluralAlias |
| 数据源测试 | 仅允许「重新测试连接」，不允许修改连接参数 |

### 2.3 不可修改项

| 不可修改项 | 说明 |
|------------|------|
| AppKey | 应用唯一标识，创建后不可改 |
| DataSourceId | 数据源绑定，创建后不可改 |

---

## 三、信息架构与路由

### 3.1 三段路由结构

```
/login                      → 登录
/register                   → 注册

/console                    → 平台控制台（ConsoleLayout，纯顶部导航）
/console/apps               → 应用卡片网格
/console/datasources        → 平台数据源管理
/console/migration          → 数据迁移中心
/console/settings/users     → 平台用户管理
/console/settings/roles     → 平台角色管理
/console/settings/depts     → 平台部门管理

/apps/:appId                → 应用工作台（AppWorkspaceLayout，左侧边栏）
/apps/:appId/dashboard      → 应用仪表盘
/apps/:appId/forms          → 表单管理
/apps/:appId/builder        → 低代码设计器
/apps/:appId/approval       → 审批流
/apps/:appId/workflow       → 工作流
/apps/:appId/settings       → 应用设置
/apps/:appId/settings/datasource  → 数据源（只读 + 测试连接）
/apps/:appId/settings/sharing     → 数据共享策略
/apps/:appId/settings/aliases     → 实体别名

/settings/*                 → 原有系统设置（保持兼容）
```

### 3.2 登录后默认跳转

登录成功后跳转至 `/console`（不再跳转至 `/profile`）。

### 3.3 布局差异

| 布局 | 导航方式 | 适用场景 |
|------|----------|----------|
| **ConsoleLayout** | 纯顶部横向导航，无侧边栏 | 平台控制台、应用列表、数据源、迁移 |
| **AppWorkspaceLayout** | 左侧边栏 + 顶部栏 | 进入某应用后的工作空间 |

---

## 四、数据模型

### 4.1 LowCodeApp 扩展

```csharp
// 新增/修改属性

/// <summary>绑定的应用级数据源 ID（null 表示使用平台默认数据源）</summary>
/// <remarks>创建后不可更改，仅构造函数可设</remarks>
public long? DataSourceId { get; private set; }

/// <summary>是否继承平台用户池</summary>
public bool UseSharedUsers { get; private set; } = true;

/// <summary>是否继承平台角色</summary>
public bool UseSharedRoles { get; private set; } = true;

/// <summary>是否继承平台部门</summary>
public bool UseSharedDepartments { get; private set; } = true;
```

### 4.2 TenantDataSource 扩展

```csharp
// 新增属性

/// <summary>所属应用 ID（null=平台级；有值=应用专属）</summary>
public long? AppId { get; set; }

/// <summary>连接池最大连接数</summary>
public int MaxPoolSize { get; set; } = 100;

/// <summary>连接超时（秒）</summary>
public int ConnectionTimeoutSeconds { get; set; } = 30;

/// <summary>最近一次测试结果</summary>
public bool? LastTestSuccess { get; set; }
public DateTimeOffset? LastTestedAt { get; set; }
```

### 4.3 AppEntityAlias（新增实体）

```csharp
/// <summary>应用内实体显示别名（创建后可修改）</summary>
public sealed class AppEntityAlias : EntityBase
{
    public long AppId { get; set; }

    /// <summary>"user" | "role" | "department"</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>单数别名，如"员工"</summary>
    public string SingularAlias { get; set; } = string.Empty;

    /// <summary>复数别名，如"员工列表"（可选）</summary>
    public string? PluralAlias { get; set; }
}
```

### 4.4 关系示意

```
Tenant (租户)
  ├── TenantDataSource [AppId = null]  ← 平台级数据源
  │
  └── LowCodeApp (应用)
        ├── DataSourceId → TenantDataSource [AppId = AppId] ← 应用专属（创建后不可改）
        ├── UseSharedUsers / Roles / Departments
        └── AppEntityAlias[] ← 实体别名（可修改）
```

---

## 五、应用创建向导（三步）

数据源在创建时一次性决定，必须通过**三步向导**完成。

### 5.1 步骤 1：基本信息

| 字段 | 必填 | 说明 |
|------|------|------|
| 应用名称 | ✓ | 显示名称 |
| 应用标识 (AppKey) | ✓ | 唯一，创建后不可改 |
| 描述 | - | |
| 图标 / 分类 | - | |

### 5.2 步骤 2：绑定数据源（创建后不可更改）

| 选项 | 说明 |
|------|------|
| 使用平台默认数据源 | DataSourceId = null |
| 选择已有数据源 | 从租户下已有数据源下拉选择 |
| 创建新数据源 | 填写连接参数，测试通过后创建并绑定 |

**必显提示**：`⚠️ 数据源绑定后不可更改，请谨慎选择`

**校验**：若选择「创建新数据源」，必须「测试连接」通过后才能进入下一步。

### 5.3 步骤 3：数据共享策略与别名（创建后可修改）

| 配置项 | 说明 |
|--------|------|
| 用户账号来源 | 平台共享 / 应用独立 |
| 角色权限来源 | 平台共享 / 应用独立 |
| 部门组织来源 | 平台共享 / 应用独立 |
| 实体别名 | user→员工、role→岗位、department→部门 等（可选） |

**校验**：若 UseSharedXxx = false，则 DataSourceId 必须非空（独立数据需独立数据源存储）。

---

## 六、应用设置页

### 6.1 数据源（只读 + 测试）

```
🔒 数据源（不可更改）

类型：MySQL
服务器：192.168.1.100:3306
数据库：order_db
状态：● 连接正常

[重新测试连接]   ← 仅允许测试，不允许修改连接参数
```

### 6.2 数据共享策略（可修改）

```
用户账号  [● 继承平台  ○ 应用独立]
角色权限  [● 继承平台  ○ 应用独立]
部门组织  [● 继承平台  ○ 应用独立]

⚠️ 从「共享」切换为「独立」时，将从平台数据初始化一份副本。
   切换前请确认应用专属数据源已配置。

[保存配置]
```

### 6.3 实体别名（可修改）

```
用户(user)   称为  [员工    ]  列表称为  [员工列表    ]
角色(role)   称为  [岗位    ]  列表称为  [岗位列表    ]
部门(dept)   称为  [部门    ]  列表称为  [部门列表    ]

[保存]
```

---

## 七、API 设计

### 7.1 应用相关

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/v1/apps` | 创建应用，请求体含 `dataSourceId`、`useShared*`、`aliases` |
| PUT | `/api/v1/apps/{id}` | 更新应用，**不含** `dataSourceId`、`appKey` |
| GET | `/api/v1/apps/{id}/datasource` | 获取应用数据源（只读，脱敏） |
| POST | `/api/v1/apps/{id}/datasource/test` | 测试连接 |
| GET | `/api/v1/apps/{id}/sharing-policy` | 获取共享策略 |
| PUT | `/api/v1/apps/{id}/sharing-policy` | 更新共享策略 |
| GET | `/api/v1/apps/{id}/entity-aliases` | 获取实体别名 |
| PUT | `/api/v1/apps/{id}/entity-aliases` | 更新实体别名 |

### 7.2 数据迁移

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/v1/migration/dry-run` | 预演迁移 |
| POST | `/api/v1/migration/execute` | 执行迁移 |

### 7.3 平台数据源

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/tenant-datasources?scope=platform` | 平台级数据源列表 |
| GET | `/api/v1/tenant-datasources?scope=app&appId={id}` | 应用专属数据源 |

---

## 八、SqlSugar 多驱动支持

### 8.1 NuGet 包补充

```xml
<!-- Atlas.Infrastructure.csproj -->

<PackageReference Include="Microsoft.Data.SqlClient" Version="6.0.1" />
<PackageReference Include="MySqlConnector" Version="2.4.0" />
<PackageReference Include="Npgsql" Version="9.0.4" />
<!-- Oracle 按需：Oracle.ManagedDataAccess.Core -->
```

### 8.2 支持的 DbType

| 类型 | 枚举值 | 说明 |
|------|--------|------|
| SQLite | Sqlite | 内置支持 |
| SQL Server | SqlServer | Microsoft.Data.SqlClient |
| MySQL | MySql | MySqlConnector |
| PostgreSQL | PostgreSQL | Npgsql |
| Oracle | Oracle | Oracle.ManagedDataAccess.Core（可选） |

### 8.3 数据源表单（按类型动态渲染）

- SQLite：文件路径
- SQL Server：服务器、端口(1433)、数据库、用户名、密码、加密选项
- MySQL：服务器、端口(3306)、数据库、用户名、密码
- PostgreSQL：服务器、端口(5432)、数据库、用户名、密码

密码字段：只写不读，存储时 AES-256 加密。

---

## 九、数据迁移中心（/console/migration）

### 9.1 流程

1. 选择来源应用
2. 选择目标应用
3. 选择迁移类型：用户账号、角色定义、部门组织、字典数据
4. 冲突策略：跳过重复 / 覆盖更新 / 中止
5. 预演 (Dry Run) → 显示影响条数
6. 执行迁移 → 进度条 + 逐条结果
7. 下载迁移报告

### 9.2 约束

- 目标应用必须已配置独立数据源（若迁移独立数据）
- 来源与目标不能相同

---

## 十、实施阶段与验收标准

### Phase 1：布局框架（纯前端）

| 任务 | 验收 |
|------|------|
| T1: 新建 ConsoleLayout.vue（纯顶部导航） | 无侧边栏，顶部 Tab 可切换 |
| T2: /lowcode/apps 迁至 /console，登录后默认跳 /console | 路由与跳转正确 |
| T3: 新建 AppWorkspaceLayout.vue，/apps/:appId 路由 | 点击应用卡片进入应用工作台 |

### Phase 2：应用级数据源

| 任务 | 验收 |
|------|------|
| T4: LowCodeApp 加 DataSourceId 字段 | 迁移脚本、实体更新 |
| T5: TenantDataSource 加 AppId 字段 | 迁移脚本、实体更新 |
| T6: 后端 API /apps/:id/datasource | 只读 + 测试，无更新接口 |
| T7: 应用创建向导步骤 2（数据源选择） | 测试连接通过才能下一步 |

### Phase 3：NuGet 驱动与多驱动支持

| 任务 | 验收 |
|------|------|
| T8: 引入 SqlClient / MySqlConnector / Npgsql | 构建通过 |
| T9: 数据源表单按 DbType 动态渲染 | 切换类型时表单字段变化 |

### Phase 4：数据共享策略与实体别名

| 任务 | 验收 |
|------|------|
| T10: LowCodeApp 加 UseShared* 字段 + API | 创建向导步骤 3、应用设置可修改 |
| T11: AppEntityAlias 实体 + API | 应用设置 > 别名可修改 |
| T12: 应用设置页（数据源只读、共享可改、别名可改） | UI 符合文档描述 |

### Phase 5：数据迁移中心

| 任务 | 验收 |
|------|------|
| T13: 迁移 API + 前端迁移向导 | Dry-run、执行、报告 |

---

## 十一、等保映射

| 控制点 | 对应能力 |
|--------|----------|
| 8.1.4 访问控制 | 数据源按租户与应用授权 |
| 8.1.5 审计要求 | 数据源配置、测试、迁移留痕 |
| 8.1.6 数据安全 | 凭据加密存储、脱敏展示、密码不回显 |

---

## 十二、变更记录

| 日期 | 变更内容 |
|------|----------|
| 2025-03-07 | 初稿：平台控制台、应用数据源、共享策略、实体别名、数据源不可变约束 |
