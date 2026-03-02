# SecurityPlatform

## 简介

安全支撑平台与基础设施支撑平台的统一解决方案，面向等保2.0合规建设，采用多租户与清晰分层架构。

## 能力概览

- 身份与权限：用户、角色、权限、菜单、部门与登录令牌。
- 资产与告警：资产台账与告警查询。
- 审计：关键操作审计记录。
- 审批流：流程定义、发起、任务处理与抄送。
- 工作流：定义、实例、事件管理。

详细能力说明见 `docs/项目能力概览.md`。

## 文档

- **`docs/架构与产品能力总览.md`** — 架构与产品功能统一参考（推荐入口）
- `docs/项目能力概览.md` — 平台能力简要说明
- `docs/审批流功能说明.md` — 审批流详细说明
- `docs/contracts.md` — 接口契约
- `docs/前后端DTO对齐清单.md` — DTO 对齐追踪
- `docs/联调验收清单.md` — 联调验收项
- `等保2.0要求清单.md` — 等保2.0 合规清单

## 目录结构

- `src/backend`：后端项目与分层模块。
- `src/frontend`：前端应用。
- `docs`：项目文档。

## 架构要点

- Clean Architecture 分层：Domain / Application / Infrastructure / WebApi。
- 多租户隔离与安全策略配置。
- JWT + 证书认证、审计日志、HTTP 日志、CORS 白名单。

## 构建与运行

当前未固化构建与运行命令，待补充后在此更新。

## 初始化管理员账号

为满足真实登录场景，启动服务前需通过环境变量或安全配置提供 Bootstrap 管理员账号信息，避免在仓库中保存明文密码。

示例（PowerShell）：

```powershell
$env:Security__BootstrapAdmin__Enabled="true"
$env:Security__BootstrapAdmin__TenantId="00000000-0000-0000-0000-000000000001"
$env:Security__BootstrapAdmin__Username="admin"
$env:Security__BootstrapAdmin__Password="P@ssw0rd!"
$env:Security__BootstrapAdmin__Roles="Admin"
```

默认启动时会创建/更新管理员账号、角色、权限与菜单，并保持幂等。
