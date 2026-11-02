# 6.0 契约形态

## 文法

| 对象 | 格式 | 例 |
| --- | --- | --- |
| 操作 ID | `<资源>.<动词>`，资源复数 kebab | `users.reset-password` |
| 权限码 | `<包>.<资源>.<动作>`，资源段等于操作 ID 的资源段 | `identity.users.reset-password` |
| 错误码 | `<包大写>_<KIND>_<NNN>` | `IDENTITY_NOTFOUND_001` |
| 消息键 | `<包>.<主题>.<名称>`；操作说明 `<操作 ID>.title` / `.description`（资源段 `-` 换 `_`） | `tenant_members.list.description` |
| 页面码 / 组件路径 | `<包>.<页面>`；路由去前导斜杠 + `/index` | `identity.users` → `identity/users/index` |
| 消息主题 | `basicapp.<包>.<事件>` | `basicapp.identity.user-locked` |

操作类放在 `Operations/<资源 Pascal>/`，权限码不能借用别的资源段（想复用已有权限，就把新操作做成该资源的动词）。

## HTTP 路由（框架 `HttpRouteConvention`，前缀 `/api`）

| 动词 | 路由 |
| --- | --- |
| `list` | `GET /<资源>`；带 `PageRequest` 的是 `POST /<资源>:list` |
| `get` | `GET /<资源>/{id}`（请求带 `Id`）或查询串 |
| `create` | `POST /<资源>`，成功 201 |
| `update` | `PUT /<资源>/{id}`；单例资源（请求不带 `Id`）`PUT /<资源>` |
| `delete` | `DELETE /<资源>?ids=…&ids=…`（批量，查询串） |
| 其他写动词 | `POST /<资源>:<动词>`，带 `Id` 时 `POST /<资源>/{id}:<动词>` |
| 声明 `ReadOnly = true` 的查询 | 请求没有嵌套对象时 `GET`，字段走查询串 |

- 请求里不是本资源 ID 的字段别叫 `Id`（会被放进路径），如 `uploadId`。
- 只有 JSON 能进操作；上传、下载、预览是宿主的普通端点（`/api/files/upload`、`/api/files/{id}/content`），挂 `RequireXiHanPermission`。
- 每个操作显式 `Expose`；`Expose.Mcp | Expose.Ai`（只读目录类）与 `Expose.Grpc`（内部调用）逐个登记在 `OperationExposureTests`，改 `Expose` 不影响 OpenAPI。

## 请求与响应

- 分页：`PageRequest { page: { index, size }, filters: [{ field, operator, value }], keyword, sorts: [{ field, descending }] }`，`size` 上限 500；`PageResult<T> { items, total, page }`。可筛选、可排序的字段是后端 `Queries/` 的白名单，越界即 VALIDATION。
- 批量：删除走查询串 `ids`，其余批量动词 `POST`、请求体 `{ "ids": [...] }`（不接受顶层数组）；响应 `BatchResult { succeeded[], failed[{ id, code }] }`。
- 字段：主键 `Guid`（字符串）；租户 ID 字符串（平台 `platform`）；时间 `DateTimeOffset` UTC；`long` 在契约里是数字字符串；枚举按名称字符串，值集进 `components.schemas`。
- 没有请求体的操作用空 record，前端调 `xxx({})`。
- 请求 DTO 有缺省值的属性用 `set`；DataAnnotations 进入 Schema，`IValidatableObject` 给跨字段规则。

## 错误

- 失败是 Problem Details：`code`、`detail`（按 `Accept-Language` 本地化）、`traceId`、字段级 `errors`；状态码由框架按错误 KIND 映射（`SECURITY_DENIED_001` / `003` 为 401 带 `WWW-Authenticate`）。
- 前端只按 `code` 分支（刷新、回登录、锁屏 `IDENTITY_DENIED_002`、维护 `RUNTIME_UNAVAILABLE_003` / `KERNEL_UNAVAILABLE_001`），不按状态码或文案。
- 新错误码与消息键同时补 10 个区域；`detail` 的占位符与 `en-US` 一致。

## 权限、页面与菜单

- `PermissionDescriptor(码, 显示名, 作用侧 Platform / Tenant / Both, audited, 说明)`；平台主体与租户主体只命中各自作用侧；`audited: true` 的操作进操作日志。
- 权限目录、套餐白名单与菜单门控都从各包 `AddPermissionCatalog` / `AddPageCatalog` 汇总，没有手写的权限或按钮码清单。
- 页面登记决定菜单：页面节点的路由、组件、标题键、读权限归登记，`access.menus` 种子每次对齐；运营只能改展示字段、另建目录与外链。新页面要后端登记、种子、前端视图与文案同一提交，相关集成测试（菜单、权限、租户）的页面断言一并补。
- 角色授予的主体是 `role:<角色 ID>`，令牌的 `role` 声明是角色 ID。
- 租户：令牌 `tenant` 声明 → Baggage `xihan.tenant`；不支持查询串与 `X-Tenant-Id`。切换租户与模仿换会话，前端整页重建。

## 实时

- Hub 路径与推送方法名由 `contracts export` 另出 `realtime.json`，前端生成 `packages/contracts/realtime.ts`；载荷是各包自己的 record，按包的源生成上下文序列化。
- 推送经 `IRealtimePush` 按 Hub 路径与受众（会话、某侧用户、所有侧用户、某侧全部）发出，在调用提交后才发（瞬时事件用 `SendNowAsync`，只能在事务之外）；配了 Redis 时经 SignalR 背板跨实例，没配只在本实例内。

## 导出与生成

```bash
# 仓库根；dotnet run 以工程目录为工作目录，--out 给绝对路径
dotnet run --project backend-next/src/XiHan.BasicApp.Host -c Release -- contracts export --out "$PWD/frontend-next/contracts"
cd frontend-next && pnpm gen:contracts && pnpm contracts:check
```

- 导出不启动应用、不连数据库；从 Release 产物运行时带 `ASPNETCORE_ENVIRONMENT=Development`（否则缺库绑定）。快照构建时直接运行 `artifacts/pin/bin/XiHan.BasicApp.Host/release/XiHan.BasicApp.Host.exe contracts export --out <绝对路径>`。
- 一次重新生成的契约要拆进两笔提交时，在新 `openapi.json` 上按顶层块删掉另一笔的条目得到中间版本，用 `git hash-object -w` + `git update-index --cacheinfo` 暂存，不动工作区。
