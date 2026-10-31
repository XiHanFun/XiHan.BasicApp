# 接口对接指南

本页说明如何通过 HTTP 调用 6.0 的接口，适用于管理端以外的对接方与集成开发者。6.0 的接口全部来自后端登记的操作（`[Operation("<资源>.<动词>")]`），一个操作同时是 HTTP 端点，并可按需发布为 gRPC 方法与 MCP 工具。编写操作见[后端开发流程](./backend/development)。

::: warning 与 5.x 不兼容
6.0 没有 `ApiResponse` 信封、没有 `/Controller/Action` 风格路由、没有 `X-Timezone` 头。5.x 的对接代码需要按本页改造，不提供兼容层。
:::

## 基址与在线文档

| 地址 | 内容 |
| --- | --- |
| `/api/` | 全部操作，前缀由 `XiHan:Protocols:Http:RoutePrefix` 决定 |
| `/openapi.json` | OpenAPI 3.1 全量文档，带 `x-tagGroups`（Development 缺省打开，见 `BasicApp:ApiDocs:Enabled`） |
| `/openapi/<组名>.json` | 按包分组的文档：身份与账号、授权、租户与套餐、组织、系统设置、文件与存储、通知与消息、审计日志、运维、开放平台 |
| `/scalar` | 交互式文档，下拉切换全量与各组 |
| `/operations.proto` | 由同一份操作 Schema 生成的 gRPC 定义（只含 `Expose.Grpc` 的操作） |
| `/mcp` | MCP 端点，见 [MCP Server](./ai/mcp) |

文档的摘要与说明按 `Accept-Language` 本地化，每个操作的说明末尾写着它要求的权限码。撰写时共 347 个操作、164 个权限码。

## 路由推导

路由由操作 ID 推导（框架 `spec/conventions/operation-ids.md`）：

| 操作 | HTTP | 说明 |
| --- | --- | --- |
| `users.create` | `POST /api/users` | 成功 201 |
| `users.get` | `GET /api/users/{id}` | 请求带 `id` 即实例级 |
| `users.update` | `PUT /api/users/{id}` | |
| `users.delete` | `DELETE /api/users?ids=…&ids=…` | 批量，ID 在查询串 |
| `users.list` | `POST /api/users:list` | 带分页对象（嵌套）即 POST |
| `users.lock` | `POST /api/users:lock` | 集合级动词 |
| `users.reset-password` | `POST /api/users/{id}:reset-password` | 实例级动词 |
| `departments.tree` | `GET /api/departments:tree` | 声明只读、没有嵌套对象即 GET，字段走查询串 |
| `profile.get` | `GET /api/profile` | 单例资源 |

请求载荷 = 路由值 ∪ JSON 请求体 ∪ 查询串，合并成一个对象；同名时路由值优先。不确定时以 `/openapi.json` 或前端生成的 `packages/contracts/operations.ts` 里的 `routes` 为准。

## 响应

### 成功响应

正文就是响应 DTO（`application/json`）。`create` 返回 201，其余 200。

- 主键是 UUID v7 字符串；租户 ID 是字符串（文法 `^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$`）。
- 时间一律 UTC，ISO 8601 带 `Z`；按用户时区显示是客户端的事。
- 枚举序列化为字符串（如 `"Active"`）。
- 属性名 camelCase。

### 失败响应（RFC 9457）

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json
Content-Language: zh-CN

{
  "type": "urn:xihan:error:SETTINGS_CONFLICT_001",
  "title": "CONFLICT",
  "status": 409,
  "code": "SETTINGS_CONFLICT_001",
  "detail": "参数键 site.name 已存在。",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "details": { "key": "site.name" }
}
```

错误码文法 `<包>_<KIND>_<NNN>`，KIND 决定状态码：

| KIND | HTTP | 典型场景 |
| --- | --- | --- |
| `VALIDATION` | 400 | 请求不合法；带字段级 `errors` |
| `DENIED` | 403（`SECURITY_DENIED_001` / `003` 为 401） | 无权限、锁屏、账号停用；未认证 / 令牌无效 |
| `NOTFOUND` | 404 | 找不到 |
| `CONFLICT` | 409 | 重复、状态不允许 |
| `UNAVAILABLE` | 503 | 节流、维护、外部服务不可用（可能带 `details.retryAfter`） |
| `TIMEOUT` / `CANCELLED` | 504 / 408 | 截止时间到、调用取消 |
| 其他 | 500 | 未预期 |

字段级错误：

```json
{
  "code": "KERNEL_VALIDATION_003",
  "status": 400,
  "errors": {
    "userName": ["长度必须在 1 到 64 之间。"],
    "lines[0].sku": ["不能为空。"]
  }
}
```

客户端只依赖 `code` 与 `errors`：`detail` 已按 `Accept-Language` 本地化，可以直接展示，但不要拿它做分支。

## 请求头

| 头 | 说明 |
| --- | --- |
| `Authorization: Bearer <访问令牌>` | 匿名操作（登录、验证码、刷新等）不带 |
| `Accept-Language` | 错误文案与文档的区域，如 `zh-CN` |
| `traceparent` | W3C Trace Context，建议每个请求生成；失败响应的 `traceId` 与它对应 |
| `xihan-deadline` | 可选，RFC 3339 UTC 截止时间 |

租户不能由客户端自报：没有 `X-Tenant-Id` 头，也不认查询串，租户来自令牌的 `tenant` 声明。

## 获取令牌

### 端点

| 操作 | HTTP | 认证 |
| --- | --- | --- |
| `auth.login-config` | `GET /api/auth:login-config` | 匿名：登录方式、第三方提供方、是否要验证码 |
| `auth.captcha` | `POST /api/auth:captcha` | 匿名：SVG 图形验证码 |
| `auth.login` | `POST /api/auth:login` | 匿名：密码登录 |
| `auth.send-login-code` / `auth.login-by-code` | `POST /api/auth:send-login-code` / `:login-by-code` | 匿名：邮箱 / 手机验证码登录 |
| `auth.verify-two-factor` / `auth.send-two-factor-code` | `POST /api/auth:verify-two-factor` / `:send-two-factor-code` | 匿名：双因素第二步 |
| `auth.refresh` | `POST /api/auth:refresh` | 匿名：刷新令牌轮换 |
| `auth.me` / `auth.permissions` | `GET /api/auth:me` / `:permissions` | 登录：当前用户 / 权限码、角色、菜单树、页面码 |
| `auth.switch-tenant` | `POST /api/auth:switch-tenant` | 登录：换到成员租户或平台 |
| `auth.lock` / `auth.unlock` | `POST /api/auth:lock` / `:unlock` | 登录：锁屏 |
| `auth.logout` | `POST /api/auth:logout` | 登录：吊销当前会话 |

### 第 1 步：登录

```bash
curl -X POST http://localhost:9710/api/auth:login \
  -H 'Content-Type: application/json' -H 'Accept-Language: zh-CN' \
  -d '{"userName":"superadmin","password":"<密码>","captchaId":"<标识>","captchaCode":"<码>"}'
```

- 平台账号不填 `tenant`；租户账号填注册地租户 ID。
- 开了双因素的账号返回 `{"token": null, "twoFactor": {"ticket": "…", "methods": [...], …}}`，再用 `auth.verify-two-factor` 凭票据与码换令牌。
- 成功返回 `token`：`accessToken`（HS256 JWT，缺省 120 分钟）、`refreshToken`（不透明，缺省 7 天）、`sessionId`、`tenantId`、`mustChangePassword` 与用户资料。

### 第 2 步：携带令牌调用

```bash
curl -X POST http://localhost:9710/api/roles:list \
  -H 'Authorization: Bearer <访问令牌>' -H 'Content-Type: application/json' \
  -d '{"page":{"index":1,"size":20},"keyword":"admin"}'
```

访问令牌的声明：`sub` = 用户 ID、`tenant` = 当前租户（平台主体没有）、`sid` = 会话 ID、`role` = 本会话激活的角色、模仿会话另有 `act`。

### 第 3 步：令牌过期

| `code` | 含义 | 处理 |
| --- | --- | --- |
| `SECURITY_DENIED_001`（401） | 未认证 | 用刷新令牌调 `auth.refresh` 一次并重放；刷新也失败即回登录 |
| `SECURITY_DENIED_003`（401） | 令牌过期、签名不符、格式错 | 同上 |
| `IDENTITY_DENIED_001` | 会话已吊销（注销、改密、锁定、被强制下线） | 直接回登录，不要刷新 |
| `IDENTITY_DENIED_002` | 已锁屏 | 进锁屏界面，`auth.unlock` 解锁 |

刷新令牌每次轮换，旧的立即失效；并发请求要串行化刷新。

### 第 4 步：切换租户

`auth.switch-tenant` 续用同一个会话（`sid` 不变）、轮换刷新令牌、按新租户重算角色，返回新的令牌对。客户端换令牌后整页重载，不要在旧页面状态上继续操作。只能切到注册地租户、生效中的成员租户（含运维支持）或平台；模仿态的会话不能切换。

## 分页、筛选与批量

### 分页查询

```json
{
  "page": { "index": 1, "size": 20 },
  "filters": [
    { "field": "status", "operator": "in", "value": "Active,Locked" },
    { "field": "createdAt", "operator": "ge", "value": "2026-10-01T00:00:00Z" }
  ],
  "keyword": "zhang",
  "sorts": [{ "field": "createdAt", "descending": true }]
}
```

- `page.index` 从 1 起，`size` 1–500；`filters` 最多 32 条、`sorts` 最多 8 条。
- 运算符只有 `eq`、`ne`、`contains`、`gt`、`ge`、`lt`、`le`、`in`；每个操作只接受白名单里的字段与运算符，其余给 VALIDATION 错误。
- 受字段安全保护的字段不能筛选或排序（如 `IDENTITY_DENIED_011`）。

响应：

```json
{ "items": [ … ], "total": 128, "page": { "index": 1, "size": 20 } }
```

### 批量写操作

删除、启停、锁定、撤销一类按标识的写操作只有批量形态，单条是一元集合。响应是逐项结果：

```json
{ "succeeded": ["0193…"], "failed": [{ "id": "0193…", "code": "IDENTITY_CONFLICT_005" }] }
```

部分失败时 HTTP 仍是 200，逐项看 `failed`。

## 文件上传与下载

只有 JSON 能进操作；文件走宿主的普通端点，它们同样在 `openapi.json` 里：

| 端点 | 说明 |
| --- | --- |
| `POST /api/files/upload` | `multipart/form-data` 上传，返回与 `files.get` 相同的响应 |
| `GET /api/files/{id}/content` | 按身份读取内容，支持 `Range` |
| `POST /api/profile/avatar` | 上传头像（只要求登录，JPEG / PNG，≤ 2 MB） |
| `files.presign` | 换限时签名地址（本地存储走 `/blobs`，S3 兼容存储直连） |
| `files.start-upload` → `sign-upload-parts` → `complete-upload` | 大文件分片直传 |

## 开放接口：签名调用（无 JWT）

服务端到服务端的调用走签名开放接口（`open.*` 操作），凭证在个人中心「开放接口凭证」里申请。

### 请求头

| 头 | 必需 | 说明 |
| --- | --- | --- |
| `X-Access-Key` | 是 | AppKey |
| `X-Timestamp` | 是 | Unix 秒，与服务端相差不超过 `BasicApp:OpenPlatform:TimestampToleranceSeconds`（缺省 300） |
| `X-Nonce` | 是 | 随机串，防重放 |
| `X-Content-Sign` | 是 | 请求体明文的 SHA256（或 SHA512）小写十六进制；空体即空串的散列，GET 也要带 |
| `X-Signature` | 是 | 请求签名（十六进制或 Base64） |
| `X-Sign-Algorithm` | 否 | 须与凭证设置一致（HMAC-SHA256 / HMAC-SHA512） |
| `X-Content-Sign-Algorithm` | 否 | `SHA256`（缺省）或 `SHA512` |
| `X-Encrypt-Algorithm` / `X-Encrypt-Iv` | 否 | 请求体加密 `AES-CBC`，明文不必带 |
| `X-Encrypt-Response` | 否 | 要求加密响应 |

### 签名算法

```text
规范串 = 方法大写 \n 路径 \n 规范查询串 \n 内容签名 \n X-Timestamp 原文 \n X-Nonce
签名   = HMAC(凭证密钥的 UTF-8 字节, 规范串)
```

规范查询串：键按序数排序、同键的值也排序，键与值都做百分号编码，用 `&` 连接，没有值写 `key=`。加密为 AES-CBC + PKCS7，密钥是凭证密钥的 UTF-8 字节（正好 16 / 24 / 32 字节直接用，否则取 SHA256）。验签失败按类别返回 400 / 403 / 409，每次调用都记开放接口日志。

### 自测端点

| 操作 | HTTP | 说明 |
| --- | --- | --- |
| `open.ping` | `GET /api/open:ping` | 验签自检，返回 AppKey、归属人与服务端时间（可用来对时） |
| `open.echo` | `POST /api/open:echo` | 原样回显请求体，检查加解密 |

OAuth 2.0 / OIDC 服务端（`/api/oauth2/*`）见[开放平台](./backend/open-api)。

## 实时通信（SignalR）

Hub 地址 `/hubs/notification`，令牌放查询串 `access_token`（只在 `/hubs` 下读）。服务端推送的方法：

| 方法 | 用途 |
| --- | --- |
| `ForceLogout` | 会话被吊销（强制下线、改密、锁定…），客户端回登录 |
| `ReceiveNotification` | 新的站内通知 |
| `TaskProgress` | 后台任务进度（导出等） |
| `UserSettingChanged` | 个人设置在其他设备上改了 |

方法清单由 `contracts export` 一并导出为 `realtime.json`。详见[实时推送](./backend/realtime)。

## 下一步

- [前端契约与请求](./frontend/request) · [统一认证](./backend/authentication) · [请求生命周期](./backend/request-lifecycle)
