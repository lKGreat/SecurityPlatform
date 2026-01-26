# 接口契约

## 通用头

- `X-Tenant-Id`: 租户标识（GUID）。除 `GET /health` 与开发环境 `openapi` 外，所有接口必须提供。
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
- `NOT_FOUND`
- `SERVER_ERROR`