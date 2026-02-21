# API Contract Baseline

本文件用于冻结 `XiHan.BasicApp` 前后端第一版契约，前端通过 `src/api/contract.ts` 统一引用。

## 认证域

- `POST /auth/login`
- `POST /auth/refresh-token`
- `POST /auth/logout`
- `GET /auth/current-user`
- `GET /auth/menus`

## 系统管理域

- `GET/POST/PUT/DELETE /system/users`
- `GET/POST/PUT/DELETE /system/roles`
- `GET /system/permissions`
- `GET/POST/PUT/DELETE /system/menus`
- `GET /system/departments/tree`
- `GET /system/tenants`

## 日志域

- `POST /access-logs`
- `POST /operation-logs`
- `POST /exception-logs`
- `POST /audit-logs`

## 约定

- 优先使用 `{ code, data, message }` 响应结构。
- 对分页结果使用 `normalizePageResult` 做兼容，支持 `items/records/data` 字段。
- 当后端动态 API 路径发生调整时，只修改 `src/api/contract.ts` 与本文件。
