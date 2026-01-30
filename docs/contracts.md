# 接口契约说明

## 目标

- 统一前后端响应结构与分页模型。
- 明确多租户与认证相关的请求头。

## 通用请求头

- `Authorization: Bearer <accessToken>`：JWT 访问令牌。
- `X-Tenant-Id: <tenantId>`：租户标识（GUID）。

## 通用响应模型

### ApiResponse

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "OK",
  "traceId": "00-abc123...",
  "data": {}
}
```

字段说明：

- `success`：是否成功。
- `code`：错误码或成功码。
- `message`：错误信息或 OK。
- `traceId`：请求链路追踪 ID。
- `data`：业务数据，失败时为 `null`。

### 错误码

- `SUCCESS`：成功
- `VALIDATION_ERROR`：参数校验错误
- `UNAUTHORIZED`：未认证
- `FORBIDDEN`：无权限
- `NOT_FOUND`：资源不存在
- `SERVER_ERROR`：服务端错误
- `ACCOUNT_LOCKED`：账号锁定
- `PASSWORD_EXPIRED`：密码过期
- `TENANT_NOT_FOUND`：租户不存在
- `INVALID_CREDENTIALS`：账号或密码错误
- `TOKEN_EXPIRED`：令牌过期

## 分页模型

### PagedRequest

```json
{
  "pageIndex": 1,
  "pageSize": 10,
  "keyword": "search",
  "sortBy": "createdAt",
  "sortDesc": true
}
```

## 认证与授权契约

### 登录

`POST /auth/token`

请求（需同时携带 `X-Tenant-Id` 请求头）：

```json
{
  "username": "admin",
  "password": "P@ssw0rd!"
}
```

响应（`ApiResponse` 包装）：

```json
{
  "accessToken": "jwt-access-token",
  "expiresAt": "2026-01-30T10:00:00Z"
}
```

### 刷新令牌（使用当前登录态）

`POST /auth/refresh`

请求：无（需携带 `Authorization` 与 `X-Tenant-Id`）

响应（`ApiResponse` 包装）：

```json
{
  "accessToken": "new-jwt-access-token",
  "expiresAt": "2026-01-30T11:00:00Z"
}
```

### 当前用户

`GET /auth/me`

响应（`ApiResponse` 包装）：

```json
{
  "id": "1001",
  "username": "admin",
  "displayName": "系统管理员",
  "tenantId": "00000000-0000-0000-0000-000000000001",
  "roles": ["Admin"],
  "permissions": ["workflow:design", "system:admin"]
}
```

### 注销

`POST /auth/logout`

请求：无（需携带 `Authorization` 与 `X-Tenant-Id`）

响应：通用 `ApiResponse`

## 菜单与权限契约

### 菜单树

`GET /api/menus`

响应：

```json
[
  {
    "id": "menu-100",
    "name": "主菜单",
    "path": "/",
    "icon": "home",
    "order": 1,
    "children": [
      {
        "id": "menu-110",
        "name": "工作流设计器",
        "path": "/workflow/designer",
        "icon": "workflow",
        "order": 10,
        "permissionCode": "workflow:design"
      }
    ]
  }
]
```

### 权限列表

`GET /api/permissions`

响应：

```json
[
  {
    "id": "perm-200",
    "code": "workflow:design",
    "name": "工作流设计",
    "module": "Workflow"
  }
]
```

字段说明：

- `pageIndex`：页码，从 1 开始。
- `pageSize`：每页数量。
- `keyword`：关键字检索。
- `sortBy`：排序字段。
- `sortDesc`：是否降序。

### PagedResult

```json
{
  "items": [],
  "total": 100,
  "pageIndex": 1,
  "pageSize": 10
}
```

字段说明：

- `items`：当前页数据。
- `total`：总数量。
- `pageIndex`：页码。
- `pageSize`：每页数量。

## 示例：分页响应包装

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "OK",
  "traceId": "00-abc123...",
  "data": {
    "items": [],
    "total": 0,
    "pageIndex": 1,
    "pageSize": 10
  }
}
```

## 可视化模块契约

### 流程概览

`GET /api/visualization/overview`

响应数据：

```json
{
  "totalProcesses": 0,
  "runningInstances": 0,
  "blockedNodes": 0,
  "alertsToday": 0,
  "riskHints": []
}
```

### 流程列表与详情

`GET /api/visualization/processes?pageIndex=1&pageSize=10`

```json
{
  "items": [
    {
      "id": "1",
      "name": "示例流程",
      "version": 1,
      "status": "Draft",
      "publishedAt": "2026-01-30T10:00:00Z"
    }
  ],
  "total": 0,
  "pageIndex": 1,
  "pageSize": 10
}
```

`GET /api/visualization/processes/{id}`

```json
{
  "id": "1",
  "name": "示例流程",
  "version": 1,
  "status": "Draft",
  "publishedAt": "2026-01-30T10:00:00Z",
  "definitionJson": "{ \"nodes\": [], \"edges\": [] }"
}
```

### 保存草稿 / 发布

`POST /api/visualization/processes`

```json
{
  "processId": "1",
  "version": 1,
  "status": "Draft"
}
```

`POST /api/visualization/processes/publish`

```json
{
  "processId": "1",
  "version": 1,
  "status": "Published"
}
```

### 运行态实例

`GET /api/visualization/instances?pageIndex=1&pageSize=10&processId=1&status=Running`

```json
{
  "items": [
    {
      "id": "1001",
      "flowName": "示例流程",
      "status": "Running",
      "currentNode": "审批节点",
      "startedAt": "2026-01-30T10:00:00Z",
      "durationMinutes": 35
    }
  ],
  "total": 0,
  "pageIndex": 1,
  "pageSize": 10
}
```

### 指标聚合

`GET /api/visualization/metrics`

```json
{
  "totalProcesses": 0,
  "draftProcesses": 0,
  "runningInstances": 0,
  "completedInstances": 0,
  "pendingTasks": 0,
  "overdueTasks": 0,
  "assetsTotal": 0,
  "alertsToday": 0,
  "auditEventsToday": 0
}
```

### 审计查询

`GET /api/visualization/audit?pageIndex=1&pageSize=10`

```json
{
  "items": [
    {
      "id": "1",
      "actor": "admin",
      "action": "可视化流程-发布",
      "result": "成功",
      "target": "流程ID: 1",
      "ipAddress": "127.0.0.1",
      "userAgent": "Mozilla/5.0",
      "occurredAt": "2026-01-30T10:00:00Z"
    }
  ],
  "total": 0,
  "pageIndex": 1,
  "pageSize": 10
}
```

## Workflow Designer APIs (草案)
- POST `/approval/flows` : 保存流程定义
  - body: { tenantId: string, definition: FlowDefinition }
  - resp: { id: string, version?: number }
- GET `/approval/flows/{id}` : 加载流程定义
  - resp: { definition: FlowDefinition }
- PUT `/approval/flows/{id}` : 更新流程定义
  - body 同保存
  - resp: { success: boolean }
- POST `/approval/flows/{id}/publish` : 发布流程
  - resp: { success: boolean, version: number }
- POST `/approval/flows/validate` : 前/后端联合校验
  - body: { tenantId: string, definition: FlowDefinition }
  - resp: { isValid: boolean, errors: string[], warnings?: string[] }
- POST `/approval/flows/{id}/preview` : 预览（返回节点线性/树形展开视图数据）
  - resp: { definition: FlowDefinition, preview: any }

### FlowDefinition / FlowNode (见前端 types/workflow.ts)
- 节点类型：start/end/approve/condition/parallel/parallel-join/copy/task
- 审批人规则：fixedUser/role/departmentLeader/selfSelect/hrbp/formField/outsideApi
- 条件：ConditionGroup{ relation AND/OR, items: [{ field, op, value, group }] }
- 按钮：NodeButton { pageType, buttonType, name, remark }

### 约束与校验（后端应补充验证）
- 必须存在唯一 start 与 end 节点
- 并行节点必须有聚合节点 parallel-join
- condition 节点需有默认分支或全覆盖
- 审批节点必须配置 approverRule
- 节点 name/code 长度与特殊字符校验
- 发布前需要通过 validate
