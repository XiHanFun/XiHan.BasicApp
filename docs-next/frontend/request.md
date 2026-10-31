# 契约与请求

本页说明管理端的契约生成物、操作调用、请求头与错误处理，适用于前端开发者与修改后端契约的开发者。

6.0 前端没有手写的 API 层：一份 `openapi.json`、一个生成器、一个运行时客户端。

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

`scripts/gen-contracts.mjs` 读 `contracts/openapi.json`：

| 文件 | 内容 |
| --- | --- |
| `types.ts` | `openapi-typescript` 生成的 `paths` / `components` / `operations` 类型；DataAnnotations 折进 Schema 的约束（`minLength`、`maximum` …）随之进入类型 |
| `operations.ts` | 路由表 `routes`（方法、模板、路由字段、查询串字段、成功码）与按资源分组的调用函数 |
| `permissions.ts` | 权限码字面量联合 `Permission`、操作 → 权限 / 策略映射 `operationPermission` / `operationPolicy`、权限定义表 `permissionDefinitions`（码、显示名、说明） |
| `enums.ts` | 字符串枚举的成员数组与类型（如 `UserStatus = ['Active', 'Disabled', 'Locked']`） |
| `realtime.ts` | Hub 路径与推送方法名 |

`paging.ts` 是手写的：把 SchemaPage 的查询参数转成 6.0 的 `PageRequest`、把 `PageResult` 转回 SchemaPage 读的形状。

生成物入库；CI 先用后端重新导出 `openapi.json` 比对，再重新生成比对（`pnpm contracts:check`），任一漂移即失败。改契约的顺序：后端操作 → 导出 → 生成 → 改页面 → 同一提交。

## 调用

```ts
import { users } from '~/contracts'

const page = await users.list({ page: { index: 1, size: 20 }, keyword: 'zhang' })
const user = await users.get({ id })
const result = await users.lock({ ids: [id] })        // BatchResult：{ succeeded, failed }
```

每个函数的签名是 `(request: RequestOf<'<操作 ID>'>, options?: CallOptions) => Promise<ResponseOf<'<操作 ID>'>>`。`invoke` 按路由表拼请求：路由字段进路径、`GET` / `DELETE` 的其余字段进查询串、其余进 JSON 请求体。

`CallOptions`：

| 项 | 说明 |
| --- | --- |
| `signal` | 取消 |
| `deadline` | 截止时间，写进 `xihan-deadline` |
| `locale` | 覆盖本次调用的区域 |
| `headers` | 额外请求头 |
| `anonymous` | 匿名调用（登录、刷新）：不带令牌，未认证也不走刷新钩子 |

### 文件

上传、下载这类普通端点不进 `operations.ts` 的调用函数，用 `~/request` 的 `uploadForm(path, form)`（multipart，带令牌、区域、`traceparent`，未认证时同样先刷新再重放，可报告进度）与 `endpointUrl(path)`。下载与预览优先换签名地址（`files.presign`）。

## 请求头

| 头 | 来源 |
| --- | --- |
| `Authorization: Bearer` | `getToken()`，匿名调用不带 |
| `Accept-Language` | 当前区域 |
| `traceparent` | 每个请求新生成（W3C） |
| `xihan-deadline` | 可选 |

不再有 `X-Timezone`、`X-Language`、`X-Request-Id` 与签名 / 加密头。

## 错误

失败统一抛 `ApiError { code, status, detail, traceId, errors?, details? }`（由 Problem Details 解包）。`detail` 已按 `Accept-Language` 本地化、可直接展示；分支只看 `code`。运行时客户端按错误码触发钩子：

| `code` | 钩子 | 行为 |
| --- | --- | --- |
| `SECURITY_DENIED_001` / `SECURITY_DENIED_003` | `onUnauthenticated` | 用刷新令牌换一次并重放；刷新也失败即登出 |
| `IDENTITY_DENIED_001` | `onSessionInvalid` | 会话已吊销，清会话回登录页 |
| `IDENTITY_DENIED_002` | `onLocked` | 拉起锁屏 |
| `RUNTIME_UNAVAILABLE_003`、`KERNEL_UNAVAILABLE_001` | `onMaintenance` | 维护提示（一批请求同时撞上只提示一次） |

字段级 `errors`（字段键 → 消息数组）交给 `XEditModal` 挂回对应字段。

## 配置

`configureRequest({ baseUrl, getToken, getLocale, hooks })` 在应用启动时注入。`baseUrl` 取 `VITE_API_BASE_URL`，留空即同源；开发态经 Vite 代理到后端。

## 时间与 ID

主键是 UUID 字符串，前端不需要数字字符串换算。时间是带 `Z` 的 UTC ISO 8601 字符串，管理端用 `~/utils` 的 `formatDate` 按应用时区显示（[国际化与时区](./i18n#时区)）。

## 下一步

- [Schema 驱动页面](./schema-page) · [接口对接指南](../api-guide) · [权限与脱敏](./permission)
