# 接口契约

## 通用头

- `X-Tenant-Id`: 租户标识（GUID）。匿名请求除 `GET /health` 与开发环境 `openapi` 外必须提供；已认证请求可省略，但若提供必须与JWT租户一致。
- `Authorization`: `Bearer <JWT>`。受保护接口必须提供。

## 通用响应

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "OK",
  "traceId": "00-...",
  "data": {}
}
```

字段说明：
- `success`: 是否成功。
- `code`: 业务错误码。
- `message`: 描述信息。
- `traceId`: 追踪标识。
- `data`: 业务数据。

## 分页请求

```json
{
  "pageIndex": 1,
  "pageSize": 10,
  "keyword": "",
  "sortBy": "",
  "sortDesc": false
}
```

## 分页响应

```json
{
  "items": [],
  "total": 0,
  "pageIndex": 1,
  "pageSize": 10
}
```

## 统一错误码

- `SUCCESS`
- `VALIDATION_ERROR`
- `UNAUTHORIZED`
- `FORBIDDEN`
- `ACCOUNT_LOCKED`
- `PASSWORD_EXPIRED`
- `NOT_FOUND`
- `SERVER_ERROR`
