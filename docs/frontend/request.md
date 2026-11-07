# 契约与请求

本页说明前端的契约生成物、操作调用、请求头与错误处理，适用于前端开发者与修改后端契约的开发者。

前端没有手写的 API 层：一份 `openapi.json`、一个生成器、一个运行时客户端。

```text
后端操作登记 ──contracts export──> contracts/openapi.json + realtime.json
                                            │  pnpm gen:contracts
                                            ▼
                         packages/contracts/{types,operations,permissions,enums,realtime}.ts
                                            │  import { users } from '~/contracts'
                                            ▼
                         packages/request：invoke → fetch → ApiError
```

## 生成物

`scripts/gen-contracts.mjs` 读 `contracts/openapi.json` 与 `contracts/realtime.json`：

| 文件 | 内容 |
| --- | --- |
| `types.ts` | `openapi-typescript` 生成的 `paths` / `components` / `operations` 类型；DataAnnotations 折进 Schema 的约束（`minLength`、`maximum` …）随之进入类型 |
| `operations.ts` | 路由表 `routes`（方法、模板、路由字段、查询串字段、成功码）、`RequestOf` / `ResponseOf` 类型与按资源分组的调用函数 |
| `permissions.ts` | 权限码字面量联合 `Permission`、操作 → 权限 / 策略映射 `operationPermission` / `operationPolicy`、权限定义表 `permissionDefinitions`（码与英文的显示名、说明；界面显示取 `permissions.list`） |
| `enums.ts` | 字符串枚举的成员数组与类型（如 `UserStatus = ['Active', 'Disabled', 'Locked']`） |
| `realtime.ts` | Hub 路径 `RealtimeHubs`、推送方法名 `RealtimeMethods`、客户端可调方法名 `RealtimeInvocations` |

`paging.ts` 是手写的：把 SchemaPage 的查询参数转成后端的 `PageRequest`、把分页结果转回 SchemaPage 读的形状，见 [Schema 驱动页面](./schema-page#资源适配器)。

生成物不要手改。CI 先用后端重新导出 `contracts/*` 比对，再重新生成比对（`pnpm contracts:check`），任一漂移即失败。改契约的顺序：后端操作 → 导出 → 生成 → 改页面与测试 → 同一提交。

## 调用

```ts
import { users } from '~/contracts'

const page = await users.list({ page: { index: 1, size: 20 }, keyword: 'zhang' })
const user = await users.get({ id })
const result = await users.lock({ ids: [id] }) // BatchResult：{ succeeded, failed: [{ id, code, message }] }
```

每个函数的签名是 `(request: RequestOf<'<操作 ID>'>, options?: CallOptions) => Promise<ResponseOf<'<操作 ID>'>>`，操作 ID `<资源>.<动词>` 的资源段换成驼峰作对象名（`export-tasks.submit` → `exportTasks.submit`）。没有请求参数的操作照样传 `{}`。

`invoke` 按路由表拼请求：路由字段进路径；登记为查询串的字段，以及 `GET` / `DELETE` 的其余标量字段进查询串（数组按同名参数重复）；其余字段作为 JSON 请求体。路由由后端的 HTTP 约定决定，常见形态：

| 动词 | HTTP |
| --- | --- |
| `list`（带 `PageRequest`） | `POST /api/<资源>:list` |
| `list`（不分页） | `GET /api/<资源>` |
| `get` | `GET /api/<资源>/{id}` |
| `create` | `POST /api/<资源>`，成功码 201 |
| `update` | `PUT /api/<资源>/{id}` |
| `delete` | `DELETE /api/<资源>?ids=…` |
| 其他写动词 | `POST /api/<资源>:<动词>` |

以 `operations.ts` 的 `routes` 为准，不手写路由。

`CallOptions`：

| 项 | 说明 |
| --- | --- |
| `signal` | 取消 |
| `deadline` | 截止时间（`Date`），以 UTC ISO 8601 写进 `xihan-deadline` |
| `locale` | 覆盖本次调用的区域 |
| `headers` | 额外请求头 |
| `anonymous` | 匿名调用（登录、刷新、注册、找回密码）：不带令牌，未认证也不走刷新钩子 |

### 普通端点

上传、下载这类普通端点不进 `operations.ts`，用 `~/request` 的：

| 函数 | 说明 |
| --- | --- |
| `uploadForm(path, form, { onProgress?, signal? })` | multipart 上传：带令牌、区域与 `traceparent`，未认证时先刷新再重放一次，可报告进度 |
| `downloadEndpoint(path)` | 下载：同样带令牌并在未认证时重放一次，返回 `{ blob, fileName }`，再交给 `~/utils` 的 `downloadBlob` |
| `endpointUrl(path)` | 普通端点或后端签发的相对地址补上接口前缀 |

文件的下载与预览优先换签名地址（`files.presign`），签名地址不带令牌也能取。

## 请求头

| 头 | 来源 |
| --- | --- |
| `Authorization: Bearer` | `getToken()`，匿名调用不带 |
| `Accept-Language` | 当前区域（缺省 `en-US`） |
| `traceparent` | 每个请求新生成（W3C） |
| `xihan-deadline` | 传了 `deadline` 才有 |

## 错误

失败统一抛 `ApiError`（由 Problem Details 解包），字段 `code`、`status`、`detail`、`traceId`、`errors?`、`details?`：

- `detail` 已按 `Accept-Language` 本地化、可直接展示，`message` 与它相同，不带错误码；
- 分支只看 `code`（`<前缀>_<KIND>_<NNN>`，如 `ACCESS_DENIED_006`），`kind` 取中段（`VALIDATION`、`NOTFOUND`、`DENIED`…）；
- 响应没有 `code` 时按状态码补：401 → `SECURITY_DENIED_001`，404 → `PROTOCOLS_NOTFOUND_000`，其余 `KERNEL_UNEXPECTED_000`；
- 字段级 `errors` 是字段键 → 消息数组，`hasFieldErrors` / `fieldError(field)` 取用，页面用 `XhFieldRoot :invalid` + `XhFieldErrorText` 挂回对应字段；
- 批量结果的失败项带本地化的 `message`，`throwOnFailed(result)`（`~/request`）把不同的原因拼成一条报错抛出，不把部分失败当成功。

运行时客户端按错误码触发钩子：

| `code` | 钩子 | 行为 |
| --- | --- | --- |
| `SECURITY_DENIED_001` / `SECURITY_DENIED_003` | `onUnauthenticated` | 用刷新令牌换一次（并发请求共用同一次刷新）并重放；刷新也失败即回登录页 |
| `IDENTITY_DENIED_001` | `onSessionInvalid` | 会话已吊销，清会话回登录页 |
| `IDENTITY_DENIED_002` | `onLocked` | 按 `details.reason` 拉起锁屏或强制改密表单 |
| `RUNTIME_UNAVAILABLE_003`、`KERNEL_UNAVAILABLE_001` | `onMaintenance` | 维护提示（10 秒内只提示一次） |

页面不重复实现这些处理。

## 配置

`main.ts` 在启动时调用 `configureRequest({ baseUrl, getToken, getLocale, hooks })`。`baseUrl` 取 `VITE_API_BASE_URL`，留空即同源；开发态经 Vite 代理到后端。

## 时间与 ID

主键是 UUID 字符串；后端的 `long` 在报文里是数字（契约类型 `number`）。时间是 UTC 的 ISO 8601 字符串，前端用 `~/utils` 的 `formatDate` 按应用时区显示（见[国际化与时区](./i18n#时区)）。

## 下一步

- [Schema 驱动页面](./schema-page) · [接口对接指南](../api-guide) · [权限与脱敏](./permission)
