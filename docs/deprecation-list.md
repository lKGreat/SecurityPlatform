# Deprecated API 清单（Sprint 1 基线冻结）

> 生效时间：2026-03-07  
> 弃用窗口：6 个月（至 2026-09-07）  
> 规则：窗口期内旧接口仅允许安全修复与关键缺陷修复，不新增功能。

## 1. 控制器级弃用映射

| 旧控制器 | 旧前缀路由 | 新控制器/分组 | 新前缀路由 | 状态 |
|---|---|---|---|---|
| `LowCodeAppsController`（部分） | `/api/v1/lowcode-apps` | `AppManifestController` | `/api/v1/app-manifests` | Deprecated |
| `LicenseController` | `/api/v1/license` | `LicenseGrantController` | `/api/v1/licenses` | Deprecated |

## 2. 旧路由到新路由映射（首批）

| 旧路由 | 新路由 | 备注 |
|---|---|---|
| `GET /api/v1/lowcode-apps` | `GET /api/v1/app-manifests` | 应用清单分页查询 |
| `POST /api/v1/lowcode-apps` | `POST /api/v1/app-manifests` | 创建应用清单 |
| `GET /api/v1/lowcode-apps/{id}` | `GET /api/v1/app-manifests/{id}` | 应用详情 |
| `PUT /api/v1/lowcode-apps/{id}` | `PUT /api/v1/app-manifests/{id}` | 更新应用 |
| `POST /api/v1/lowcode-apps/{id}/publish` | `POST /api/v1/app-manifests/{id}/releases` | 旧发布动作迁移到发布中心 |
| `POST /api/v1/license/activate` | `POST /api/v1/licenses/import` | 导入并激活授权 |
| `GET /api/v1/license/status` | `GET /api/v1/licenses/validate` | 查询授权有效性 |
| `GET /api/v1/license/fingerprint` | `POST /api/v1/licenses/offline-request` | 离线申请（新模型带上下文） |

## 3. 前端路由兼容策略

| 旧路由 | 新路由 | 状态 |
|---|---|---|
| `/settings/*` | `/console/settings/*` | Deprecated + Redirect |

