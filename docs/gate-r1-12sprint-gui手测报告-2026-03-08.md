# Gate-R1 手工测试报告（12 Sprint 产品化重构）

## 1. 基本信息

- 测试日期：2026-03-08
- 测试环境：Cursor Cloud Linux（kernel 6.1.147）
- 后端地址：`http://localhost:5000`
- 前端地址：`http://localhost:5173`
- 测试账号：`admin`（tenant: `00000000-0000-0000-0000-000000000001`）
- 代码基线：`cursor/security-platform-productization-cbff`（含本次启动修复提交）

## 2. 执行范围

按 Gate-R1 要求覆盖以下链路：

1. 平台控制面（`/console`、`/console/apps`、`/console/tools`）
2. 应用工作台（`/apps/:appId/dashboard`、`/apps/:appId/settings`）
3. 运行交付面（`/r/:appKey/:pageKey`）
4. License 治理页（`/settings/license`）
5. 关键 API 补充验证（`platform/app-manifests/packages/licenses/tools/runtime`）

## 3. 测试结果总览

| 场景 | 结果 | 说明 |
|---|---|---|
| 平台控制面可访问 | 通过 | 控制台首页、应用页、工具授权页可进入 |
| 应用创建向导入口 | 通过 | `/console/apps` 可打开“新建应用”向导 |
| 应用工作台页面可达 | 部分通过 | 页面可达，但业务数据请求受 License 中间件拦截 |
| 运行态路由可达 | 部分通过 | `/r/:appKey/:pageKey` 可打开，但运行数据受 License 拦截 |
| License 管理页可访问 | 通过 | 页面渲染正常，支持上传证书入口 |
| 关键 API 可用性 | 未通过（受阻） | 除 `auth`/`license` 白名单外，多数端点返回 `402 LICENSE_INVALID` |

## 4. 证据留痕

### 4.1 GUI 截图证据

目录：`docs/evidence/gate-r1-20260308/`

- `01-console-home.png`
- `02-console-apps.png`
- `03-console-tools.png`
- `04-license-center.png`
- `05-app-create-wizard.png`
- `07-app-dashboard.png`
- `08-app-settings.png`
- `09-runtime-route.png`

### 4.2 API 补充验证证据

- `docs/evidence/gate-r1-20260308/api-check-results.json`

关键结果摘要：

- `POST /api/v1/auth/token`：`200 SUCCESS`
- `GET /api/v1/license/status`：`200 SUCCESS`
- `GET /api/v1/platform/overview`：`402 LICENSE_INVALID`
- `GET /api/v1/app-manifests`：`402 LICENSE_INVALID`
- `POST /api/v1/packages/export`：`402 LICENSE_INVALID`
- `POST /api/v1/licenses/offline-request`：`402 LICENSE_INVALID`
- `GET /api/v1/tools/authorization-policies`：`402 LICENSE_INVALID`
- `GET /api/v1/runtime/apps/{appKey}/pages/{pageKey}`：`402 LICENSE_INVALID`

## 5. 缺陷与阻塞清单

### DEFECT-01（高）

- 现象：产品化新增治理/平台/运行态端点在未激活 License 时全部被 `LicenseEnforcementMiddleware` 拦截（`402`）。
- 影响：Gate-R1 链路无法完成业务闭环（创建应用、发布、运行、导入导出、工具策略等均不可用）。

### DEFECT-02（高）

- 现象：`/api/v1/licenses/*`（新治理 License API）也被 License 中间件拦截。
- 影响：无法通过新 API 完成离线申请/导入/校验闭环，自举路径被阻断。

### DEFECT-03（中）

- 现象：`/api/v1/secure/antiforgery` 在当前中间件白名单策略下返回 `402`。
- 影响：写接口所需 `X-CSRF-TOKEN` 无法通过标准流程获取，影响幂等+CSRF 联调验证。

### DEFECT-04（中）

- 现象：仓库内 `Bosch.http/Licenses.http` 示例证书激活返回“证书签名验证失败”。
- 影响：开发环境无法使用示例证书快速激活，Gate 测试前置条件缺失。

## 6. 结论

- **Gate-R1 当前结论：未通过（存在高优先级阻塞）**  
  已完成 GUI 路由级手测与证据留痕，但业务链路受 License 强制拦截，无法形成“平台→应用→运行→治理”完整闭环。

## 7. 建议修复顺序

1. 明确 License 中间件白名单策略：确保 License 自举链路（至少 `licenses/*` 与 `secure/antiforgery`）可执行。
2. 提供可用开发证书或可配置的开发旁路开关（仅 Development）。
3. 重新执行 Gate-R1 全链路（创建应用→发布→运行→任务/审计→治理）。
