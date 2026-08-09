# 服务端交互

前端调后端的全部约定：请求客户端怎么建、URL 怎么拼、分页协议长什么样、响应怎么解包、401/423 怎么处理。

## 三层封装

```text
业务视图
   └─ src/api/modules/**                  ← 按域的 API 与 DTO 类型（你写的）
        └─ src/api/base.ts / factory.ts   ← 动态 API 客户端 / 资源工厂
             └─ packages/request          ← RequestClient（axios 封装，拦截器 + 解包）
```

## 动态 API 客户端

| 工具 | 说明 |
| --- | --- |
| `createDynamicApiClient(controllerName)` | 按控制器名建低层客户端，暴露 `get` / `post` / `put` / `delete`；URL 拼成 `{apiPrefix}/{控制器名}/{动作名}` |
| `createReadApi(controller, resource)` | 标准读封装：`page` / `detail` |
| `createCommandApi(controller, resource)` | 标准写封装：`create` / `update` |
| `defineResource({ query, command, resource? })` | **资源工厂**，一次生成 `page`/`detail`/`create`/`update`/`remove`，并保留 `query`/`command` 客户端供扩展 |

标准 CRUD 直接用资源工厂，不要手拼：

```ts
export const userApi = defineResource<
  UserListItemDto, UserDetailDto, UserCreateDto, UserUpdateDto, UserPageRequest
>({
  query: 'UserQuery',
  command: 'User',
})
```

## 动作名 = 后端方法名剥离动词前缀

后端是[动态 API](https://framework.docs.xihanfun.com/guide/dynamic-api)，路由由方法名推导，**前端用的已经是剥离后的动作名**：

| 后端方法 | 动作名 | 实际请求 |
| --- | --- | --- |
| `GetPositionPageAsync` + `[HttpPost]` | `PositionPage` | `POST /api/PositionQuery/PositionPage` |
| `GetPositionDetailAsync` | `PositionDetail` | `GET /api/PositionQuery/PositionDetail?id=…` |
| `CreatePositionAsync` | `Position` | `POST /api/Position/Position` |
| `UpdatePositionStatusAsync` | `PositionStatus` | `PUT /api/Position/PositionStatus` |
| `SwitchTenantAsync`（无动词前缀） | `SwitchTenant` | `POST /api/Auth/SwitchTenant` |

一个域的 API 模块典型写法：

```ts
const positionQueryApi = createDynamicApiClient('PositionQuery')
const positionCommandApi = createDynamicApiClient('Position')

export const positionApi = {
  create: input => positionCommandApi.post('Position', input),
  update: input => positionCommandApi.put('Position', input),
  updateStatus: input => positionCommandApi.put('PositionStatus', input),
  delete: id => positionCommandApi.delete('Position', { id }),   // id 走查询串
  page: input => positionQueryApi.post('PositionPage', input),   // 分页 POST
}
```

::: danger id 不要拼成路径段
动态 API 的**路由段只由显式 `[FromRoute]` 参数产生**，普通参数一律落到查询串或请求体。写成 <code v-pre>delete(`Position/${id}`)</code> 会 404。
:::

## 分页统一走 POST

分页方法一律 POST，**整个查询对象作为请求体**——GET 查询串装不下嵌套的过滤与排序数组。

```ts
// POST /api/{Controller}/{Resource}Page
messageQueryApi.post<PageResult<EmailListItemDto>>('EmailPage', emailPageQueryDto)
```

请求体由 `createPageRequest` 组装，两段结构：

| 段 | 内容 |
| --- | --- |
| `conditions` | `keyword`（`{ value, fields[] }`）/ `filters`（`QueryFilter[]`，含 Between、In）/ `sorts`（`QuerySort[]`，带 `priority`） |
| `page` | `{ pageIndex, pageSize }`，默认 `1` / `20`，**`pageSize` 上限 `500`** |

返回 `PageResult<TItem>`：`items` + `page`（`totalCount` / `totalPages` / `hasNext` 等）。

辅助函数：

| 函数 | 用途 |
| --- | --- |
| `createPageRequest({ page, conditions })` | 组装请求体 |
| `querySortsFromSchema(sorts)` | Schema 页的多字段排序 → `conditions.sorts`（`priority` = 数组下标） |
| `queryFilter(field, value, operator)` | 造一条过滤 |
| `querySort(field, direction, priority)` | 造一条排序 |
| `queryKeyword(value, fields)` | 造关键字条件 |
| `compactRecord(obj)` | 去掉 `undefined` / `null` / 空串字段 |

`QueryOperator` / `SortDirection` 的完整取值表见 [接口对接指南](../api-guide#分页与查询协议)。

## RequestClient 做了什么

`packages/request/index.ts`，axios 封装。

### 请求拦截

| 注入 | 值 |
| --- | --- |
| `Authorization` | `Bearer <token>`（本地有 token 时） |
| `X-Timezone` | 用户已选时区，否则跟随浏览器 `Intl` |
| `X-Language` | 当前 locale |
| `X-Request-Id` | 前端自生成，供本地请求日志面板 |

另外两件事：检测到 `FormData` 会**删掉 `Content-Type`**（让浏览器自己带 boundary）；启用接口签名时对请求做签名与可选加密。

### 响应拦截

按顺序：

1. （可选）解密响应；
2. 记本地请求日志（含后端 `traceId`）；
3. 归一化错误消息（覆盖 axios 默认英文）；
4. **`423` → 触发锁定遮罩钩子**——必须先于 401 分支，且不刷新不登出；
5. **`401` → 刷新令牌并重放原请求**。

### 401 自动刷新的三条保障

```text
· 并发请求排队等同一次刷新，不会打出 N 个刷新请求
· 刷新请求自带 _isRefresh 标记，自身 401 时不会再次触发刷新（防死循环）
· 刷新失败 → 清 token + 调 logout 钩子重置 store + 跳登录页
```

### 响应解包

业务代码拿到的是**已解包的强类型 `data`**，看不到 `ApiResponse` 信封：

```ts
const page = await userApi.page(query)   // 直接就是 PageResult<UserListItemDto>
```

::: warning 判定成功用 `isSuccess`，不是「有没有 data」
服务端 `WhenWritingNull` 会把 `data: null` 整个省略。`RequestClient` 因此以非空布尔的 `isSuccess` 作为主判据；失败时把 `message` 抛成 `Error`，业务侧 `catch` 到的 `error.message` 就是可直接展示的中文提示。
:::

### Flat 模式

不想写 `try/catch` 时用 `requestFlat` / `getFlat` / `postFlat`，返回 `{ data, error }` 而不抛异常。

## 环境变量

| 变量 | 开发默认 | 说明 |
| --- | --- | --- |
| `VITE_API_BASE_URL` | 空 | 协议+主机。**开发态留空**走同源 Vite 代理，天然无 CORS |
| `VITE_API_PREFIX` | `/api` | 接口前缀 |
| `VITE_DEV_PROXY_TARGET` | `http://localhost:9708` | 开发态代理目标（后端地址） |
| `VITE_API_SECURITY_*` | `ENABLED=false` | 接口签名/加密开关与密钥 |

::: warning 改了后端端口
要同步改 `frontend/.env.development` 的 `VITE_DEV_PROXY_TARGET`，否则 dev server 转发到旧端口。
:::

## 排查

| 现象 | 原因 |
| --- | --- |
| 404 | 动作名写错（动词前缀没剥离）；或把 id 拼成了路径段 |
| 分页参数后端收不到 | 后端方法漏标 `[HttpPost]` |
| 一直 401 循环 | 刷新端点要同时带旧 `accessToken` 与 `refreshToken`；检查 `_isRefresh` 标记 |
| 收到 423 却跳了登录页 | 423 分支必须先于 401 处理 |
| 上传报 Content-Type 错误 | 手工设了 `Content-Type`，`FormData` 应让浏览器自己带 |
| 跨域 | 开发态把 `VITE_API_BASE_URL` 填成了后端绝对地址 |

## 相关页面

- [接口对接指南](../api-guide)：响应信封、业务码全表、请求头、获取 token
- [框架简介](./introduction#请求链路时序)：完整请求时序图
- [Schema 驱动页面](./schema-page)：查询参数怎么从页面流到这里
