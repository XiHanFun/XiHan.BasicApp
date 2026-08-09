# 接口对接指南

本页是**前后端分离对接的唯一参考**：接口基址、统一响应信封、请求头、**怎么拿到 token**、分页与查询协议、动态 API 路由推导、错误处理、开放接口签名调用。所有字段、路由与取值都对照仓库源码核实，可直接照抄。

> 想先跑起来看接口，见 [快速开始](./getting-started)；想知道认证体系的设计（会话、2FA、密码哈希），见 [身份与认证](./backend/authentication)。

## 基址与在线文档

| 环境 | 监听端口 | API 基址 | 在线文档（Scalar） |
| --- | --- | --- | --- |
| Development | `9708` | `http://127.0.0.1:9708/api` | `http://127.0.0.1:9708/scalar` |
| Production | `9708`（仓库默认） | 由部署域名决定 | 生产建议关闭 |

- 所有动态 API 都挂在 **`/api`** 前缀下。前端通过两个环境变量拼装：`VITE_API_BASE_URL`（协议+主机，开发态通常为空走 Vite 代理）与 `VITE_API_PREFIX`（默认 `/api`）。
- 实际监听以 `Hosting:Urls` 为准；该配置会覆盖 launch profile 的 `applicationUrl`。

## 统一响应信封

**所有接口**（无论成功失败）都返回同一个 JSON 信封 `ApiResponse` / `ApiResponse<T>`：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `code` | `int` | 业务码，**恒为数字**（`ApiResponse.Code` 属性上标了 `NumericEnumConverter`，属性级优先级最高，不受全局枚举转字符串影响） |
| `message` | `string` | 面向用户的提示，默认取业务码的描述文案（如「请求成功」） |
| `data` | `any` | 成功时是业务数据；失败时承载错误明细字符串 |
| `traceId` | `string?` | 请求追踪 ID，跨日志/链路定位一次请求 |
| `timestamp` | `string` | 服务端时间（UTC） |
| `isSuccess` | `bool` | `code` 落在 `[200, 300)` 视为成功 |

成功：

```json
{
  "code": 200,
  "message": "请求成功",
  "data": { "userName": "superadmin" },
  "traceId": "8f2b1c0d4e5a6b7c8d9e0f1a2b3c4d5e",
  "timestamp": "2026-08-04T02:11:33.4821567+00:00",
  "isSuccess": true
}
```

失败（**具体错因在 `data`，`message` 只是业务码的通用描述**）：

```json
{
  "code": 400,
  "message": "请求错误",
  "data": "用户名或密码错误",
  "timestamp": "2026-08-04T02:11:33.4821567+00:00",
  "isSuccess": false
}
```

::: tip 客户端解包顺序
判定成功优先读 **`isSuccess`**（非空布尔、信封必写），不要靠「有没有 `data` 字段」——服务端 `WhenWritingNull` 会把 `data: null` 整个省略。错误提示优先读 **`data`**，其次 `message`。前端 `RequestClient.request` 就是这么实现的。
:::

### 业务码全表

`code` 分两段：**协议段（100–599）与 HTTP 状态码同值同名**；**业务段（10000+）**表达更细的语义。

| 段 | 码 | 名称 | 含义 |
| --- | --- | --- | --- |
| 1xx | `100` | Continue | 继续请求 |
| | `101` | SwitchingProtocols | 切换协议 |
| 2xx | `200` | Success | 请求成功 |
| | `201` | Created | 资源创建成功 |
| | `202` | Accepted | 请求已接受（异步任务提交，如导出） |
| | `204` | NoContent | 无内容 |
| 3xx | `300` / `301` / `302` / `304` | — | 多种响应可选 / 永久重定向 / 临时重定向 / 资源未修改 |
| 4xx | `400` | BadRequest | 参数错误、格式错误或缺少必要参数 |
| | `401` | Unauthorized | 未通过身份认证，需重新登录或提供有效凭据 |
| | `403` | Forbidden | 已认证但无权访问该资源 |
| | `404` | NotFound | 资源不存在或已删除 |
| | `405` | MethodNotAllowed | 请求方法不允许 |
| | `408` | RequestTimeout | 请求超时 |
| | `409` | Conflict | 请求冲突（重复创建、乐观锁版本冲突、防重放失败） |
| | `410` | Gone | 资源已永久删除 |
| | `415` | UnsupportedMediaType | 媒体类型不支持 |
| | `422` | UnprocessableEntity | 参数格式正确但业务语义校验未通过 |
| | **`423`** | **Locked** | **会话已锁定** —— 身份仍有效，应引导解锁而**不是**跳登录页 |
| | `429` | TooManyRequests | 请求过于频繁（限流/防刷） |
| 5xx | `500` | InternalServerError | 服务器内部错误 |
| | `501` / `502` / `503` / `504` | — | 功能未实现 / 网关错误 / 服务不可用 / 网关超时 |
| 认证授权 | `10001` | LoginExpired | 登录已过期（区别于 401：曾经登录、现已过期） |
| | `10002` | TokenInvalid | 令牌无效（格式错误、签名不合法或已吊销） |
| | `10003` | TokenExpired | 令牌已过期，可用刷新令牌换新 |
| | `10004` | PermissionDenied | 缺少所需权限码（面向按钮/字段级细粒度） |
| 数据校验 | `11000` | ValidationFailed | 数据校验失败，明细逐项置于 `data` |
| 业务处理 | `12000` | BusinessFailed | 业务规则不满足、状态机不允许当前操作 |
| 数据访问 | `13000` | DatabaseError | 数据库访问异常 |
| 外部依赖 | `14000` | ThirdPartyServiceError | 第三方服务调用异常 |

### JSON 序列化约定

对接时最容易踩坑的几条（均由框架 `XiHanWebCoreMvcOptions.ConfigureJsonOptionsDefault` 统一设定）：

| 约定 | 表现 | 注意 |
| --- | --- | --- |
| **camelCase 命名** | `UserName` → `userName` | `OAuthProviders` → **`oAuthProviders`**（首字母小写只作用于第一个字符，别写成 `oauthProviders`） |
| **null 省略** | `DefaultIgnoreCondition = WhenWritingNull` | 字段可能整个不出现，客户端类型要按可选处理 |
| **`long` 序列化为字符串** | `12345` → `"12345"` | 避免 JavaScript Number 精度溢出；雪花 ID、耗时、字节数都是字符串，需要运算时显式转数字。反序列化时数字与字符串都接受 |
| **枚举序列化为成员名** | `Status.Enabled` → `"Enabled"` | 唯一例外是 `ApiResponse.code`，恒为 int。请求侧枚举既可发数字也可发成员名 |
| **`DateTime` / `DateTimeOffset` 按 `X-Timezone` 换算输出** | 存储恒 UTC，输出按请求头时区 | 不发头就按服务端默认；`DateTimeOffset` 走 ISO 8601 带偏移 |
| **忽略循环引用** | `ReferenceHandler.IgnoreCycles` | — |

## 请求头约定

| 请求头 | 必填 | 值 | 作用 |
| --- | --- | --- | --- |
| `Authorization` | 受保护接口必填 | `Bearer <accessToken>` | 身份认证 |
| `Content-Type` | 有 body 时 | `application/json` | 上传文件时用 `multipart/form-data`（此时**不要**手工设 Content-Type） |
| `X-Language` | 否 | `zh-CN` / `en-US` | 后端据此本地化 `message` 与业务文案、枚举标签 |
| `X-Timezone` | 否 | IANA 时区，如 `Asia/Shanghai` | 后端把 UTC 时间换算为该时区后输出 |
| `X-Trace-Id` | 否 | 任意串 | 入站链路 ID；未启用 OpenTelemetry 时作为 TraceId 回退来源。响应头必回该字段 |

> 前端另会带一个 `X-Request-Id`（前端自生成，用于本地请求日志面板），后端不消费它。

## 怎么获取 token（完整实操）

认证入口是 `AuthAppService`，类级 `[DynamicApi(RouteTemplate = "api/Auth")]`，因此全部端点都在 **`/api/Auth/*`** 下。核心设计是**先登录后选租户**：登录时不选租户，认证通过后由服务端按成员关系决定落点。

### 端点总表

| 端点 | 方法 | 匿名 | 说明 |
| --- | --- | --- | --- |
| `/api/Auth/LoginConfig` | GET | ✅ | 查可用登录方式与 OAuth 提供商 |
| `/api/Auth/Login` | POST | ✅ | 账号密码登录（可能返回 2FA 挑战） |
| `/api/Auth/EmailLoginCode` | POST | ✅ | 下发邮箱登录验证码 |
| `/api/Auth/EmailLogin` | POST | ✅ | 邮箱验证码登录 |
| `/api/Auth/PhoneLoginCode` | POST | ✅ | 下发短信登录验证码 |
| `/api/Auth/PhoneLogin` | POST | ✅ | 短信验证码登录 |
| `/api/Auth/RefreshToken` | POST | ✅ | 用旧 Access + Refresh 换新令牌 |
| `/api/Auth/Register` | POST | ✅ | 注册 |
| `/api/Auth/PasswordResetRequest` | POST | ✅ | 申请找回密码（防枚举，恒返回「已受理」） |
| `/api/Auth/ConsumePasswordResetToken` | POST | ✅ | 消费重置令牌设新密码 |
| `/api/Auth/UserInfo` | GET | ❌ | 当前用户资料 + 角色 + 权限码 |
| `/api/Auth/Permissions` | GET | ❌ | 权限码 + 角色 + **菜单路由**（前端动态路由数据源） |
| `/api/Auth/SwitchTenant` | POST | ❌ | 切换租户 / 进入平台运维态，换发令牌 |
| `/api/Auth/Logout` | POST | ❌ | 登出，撤销会话 |
| `/api/Auth/LockSession` | POST | ❌ | 锁屏（此后接口返回 `423`） |
| `/api/Auth/UnlockSession` | POST | ❌ | 解锁 |
| `/api/Auth/OAuthBindTicket` | POST | ❌ | 生成三方账号绑定一次性票据 |

> 路由名是方法名**剥离动词前缀并去掉 `Async` 后缀**的结果：`GetLoginConfigAsync` → `LoginConfig`、`CreateOAuthBindTicketAsync` → `OAuthBindTicket`。规则见[下文](#动态-api-路由推导规则)。

### 第 1 步：登录换令牌

```bash
curl -X POST http://127.0.0.1:9708/api/Auth/Login \
  -H "Content-Type: application/json" \
  -H "X-Language: zh-CN" \
  -d '{
    "username": "superadmin",
    "password": "SuperAdmin@123"
  }'
```

请求体 `LoginRequestDto`：

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `username` | `string` | ✅ | **邮箱**（全平台唯一登录标识）；平台账号也可用用户名 |
| `password` | `string` | ✅ | 明文密码（走 HTTPS） |
| `twoFactorCode` | `string?` | — | 开启 2FA 时的验证码 |
| `twoFactorMethod` | `string?` | — | `totp` / `email` / `phone` |
| `deviceId` | `string?` | — | 设备指纹，用于多端会话管理 |

成功响应（`data` 是 `LoginResponseDto`）：

```json
{
  "code": 200,
  "message": "请求成功",
  "data": {
    "requiresTwoFactor": false,
    "token": {
      "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9....",
      "refreshToken": "b7c1f0e9a2d34c5f8e6b0a1d2c3e4f5a",
      "tokenType": "Bearer",
      "expiresIn": 3600,
      "issuedAt": "2026-08-04 10:11:33",
      "expiresAt": "2026-08-04 11:11:33"
    }
  },
  "isSuccess": true
}
```

**需要两步验证时**不签发令牌，而是返回挑战（此时 email/phone 方式已自动下发验证码，TOTP 由认证器本地生成）：

```json
{
  "code": 200,
  "data": {
    "requiresTwoFactor": true,
    "availableTwoFactorMethods": ["totp", "email"],
    "twoFactorMethod": "totp",
    "codeSent": false,
    "token": null
  },
  "isSuccess": true
}
```

带上验证码重新请求同一端点即可完成登录：

```bash
curl -X POST http://127.0.0.1:9708/api/Auth/Login \
  -H "Content-Type: application/json" \
  -d '{ "username": "a@b.com", "password": "***", "twoFactorMethod": "totp", "twoFactorCode": "123456" }'
```

::: tip 免密码的两条路
邮箱验证码登录是两步：先 `POST /api/Auth/EmailLoginCode`（body `{"email":"a@b.com"}`，返回 `expiresInSeconds`，**开发环境**才回显 `debugCode`），再 `POST /api/Auth/EmailLogin`（body `{"email":"a@b.com","code":"123456"}`）直接拿到 `LoginTokenDto`。短信同理走 `PhoneLoginCode` / `PhoneLogin`。验证码 6 位、10 分钟、**一次性消费**（读取即销毁，无论成败）。
:::

### 第 2 步：带令牌调接口

把 `accessToken` 放进 `Authorization` 头：

```bash
curl http://127.0.0.1:9708/api/Auth/UserInfo \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...." \
  -H "X-Timezone: Asia/Shanghai"
```

`/api/Auth/UserInfo` 返回 `UserInfoDto`：`basicId`、`userName`、`nickName`、`avatar`、`email`、`phone`、`tenantId`、`isPlatform`（当前是否平台运维态）、`canAccessPlatform`、`roles[]`、`permissions[]`，外加站点品牌字段 `appTitle` / `appSubtitle` / `appLogo`。

`/api/Auth/Permissions` 返回 `PermissionInfoDto`：`roles[]`、`permissions[]`、**`menus[]`**（菜单路由树，前端据此 `addRoute` 生成动态路由）。

::: warning 权限码不在令牌里
Access Token 的 Claim 只有 `sub`/`jti`/`UserId`/`UserName`/`SessionId`/`TenantId`/`DeviceFingerprint` 与多个 `Role`；**具体权限码不冻结进令牌**（超管例外，放一个通配 `*` 作快路径）。鉴权一律走服务端**授权快照**实时判定——所以授予/回收权限**无需重新登录**即时生效，也避免权限清单随令牌泄露。
:::

### 第 3 步：令牌过期怎么办

Access Token 默认 60 分钟、Refresh Token 默认 7 天（以 `XiHan:Authentication:Jwt` 配置为准）。过期后用**旧 AccessToken + RefreshToken** 换新：

```bash
curl -X POST http://127.0.0.1:9708/api/Auth/RefreshToken \
  -H "Content-Type: application/json" \
  -d '{ "accessToken": "<旧的 accessToken>", "refreshToken": "<refreshToken>" }'
```

返回新的 `LoginTokenDto`。注意两点：

- 该端点是**匿名**的（否则过期令牌进不来），两个令牌都必须带上、缺一即失败。
- 前端 `RequestClient` 已内建自动刷新：任一请求收到 `401` 就尝试刷一次并重放原请求；刷新期间的并发请求排队等待同一次刷新结果；刷新失败即清空本地令牌、跳登录页。刷新请求自身失败不会再次触发刷新（`_isRefresh` 标记，防死循环）。

### 第 4 步：切换租户

登录后可随时切换到某个租户或回到平台运维态，**换发一套新令牌**（复用当前会话轮换令牌，不产生新的登录事件、不新增设备）：

```bash
curl -X POST http://127.0.0.1:9708/api/Auth/SwitchTenant \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d '{ "tenantId": "1001" }'      # tenantId 传 null 表示切到平台运维态
```

可切换的租户列表来自 `GET /api/TenantQuery/MyAvailableTenants`。

### 登出与锁屏

- `POST /api/Auth/Logout`：撤销会话、吊销关联令牌。
- `POST /api/Auth/LockSession`（body `{"password":"***"}`）后，后续接口返回 **`423 Locked`**。这**不是** 401：用户身份仍然有效，客户端应弹解锁遮罩而不是跳登录页；`POST /api/Auth/UnlockSession` 解锁。

### 认证相关的失败排查

| 现象 | 多半是 |
| --- | --- |
| `401` 且 `data` 提示令牌无效/过期 | 没带 `Authorization`、`Bearer ` 前缀漏了、或令牌已过期未刷新 |
| 登录返回 400 且提示账号或密码错误 | `username` 要传**邮箱**（平台账号才可用用户名） |
| `403` | 认证通过但缺权限码，或租户版本（Edition）白名单未放行该权限 |
| `423` | 会话被锁屏，先解锁 |
| 刷新令牌一直失败并被登出 | `accessToken` 与 `refreshToken` 必须成对提交，且 refresh 未过期（默认 7 天） |

## 分页与查询协议

### 分页统一走 POST

框架的动态 API 会把 `GetXxxAsync` 推导成 GET，但**分页方法一律显式标 `[HttpPost]`**，把整个查询对象作为 JSON body 发送——GET 查询串装不下嵌套的过滤/排序数组。

```bash
curl -X POST http://127.0.0.1:9708/api/UserQuery/UserPage \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d '{
    "conditions": {
      "keyword": { "value": "张", "fields": ["userName", "nickName"] },
      "filters": [
        { "field": "status", "operator": 1000, "value": "Enabled" },
        { "field": "createdTime", "operator": 4000, "values": ["2026-01-01T00:00:00", "2026-08-04T23:59:59.999"] }
      ],
      "sorts": [
        { "field": "createdTime", "direction": 1001, "priority": 0 }
      ]
    },
    "page": { "pageIndex": 1, "pageSize": 20 }
  }'
```

请求体结构（后端 `PageRequestDtoBase`，业务分页 DTO 继承它并追加自己的顶层字段）：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `conditions.keyword` | `{ value, fields[], matchMode }` | 关键字跨字段搜索，`matchMode` 默认 `Contains` |
| `conditions.filters` | `QueryFilter[]` | 通用过滤：`{ field, operator, value }`；`In` / `Between` 用 `values` 数组 |
| `conditions.sorts` | `QuerySort[]` | 多字段排序：`{ field, direction, priority }`，**`priority` 越小越优先**（前端以数组下标填充） |
| `page.pageIndex` | `int` | 页码，**从 1 开始**；小于 1 自动纠正为 1 |
| `page.pageSize` | `int` | 每页条数，默认 `20`，**上限 `500`**（超出自动截断，小于 1 回退默认值） |

`QueryOperator` 取值：

| 值 | 名称 | 值 | 名称 |
| --- | --- | --- | --- |
| `1000` | Equal | `2000` | Contains |
| `1001` | NotEqual | `2001` | StartsWith |
| `1002` | GreaterThan | `2002` | EndsWith |
| `1003` | GreaterThanOrEqual | `3000` | In |
| `1004` | LessThan | `3001` | NotIn |
| `1005` | LessThanOrEqual | `4000` | Between |
| | | `5000` / `5001` | IsNull / IsNotNull |

`SortDirection`：`1000` = 升序，`1001` = 降序。

> 枚举请求侧既接受数字也接受成员名（`"Between"`）；前端契约层统一发数字。

### 分页响应

```json
{
  "code": 200,
  "data": {
    "items": [ /* ... */ ],
    "page": {
      "pageIndex": 1, "pageSize": 20, "totalCount": 137, "totalPages": 7,
      "currentPageCount": 20, "startRecord": 1, "endRecord": 20,
      "hasPrevious": false, "hasNext": true,
      "isFirstPage": true, "isLastPage": false
    },
    "extendDatas": null
  },
  "isSuccess": true
}
```

::: warning 排序/过滤受字段级安全门控
读侧会经 `IFieldSecurityService.GuardFiltersAsync` / `GuardSortsAsync` 过滤：**只有当前用户「可读且未脱敏」的字段**才允许参与过滤与排序，其余被静默剔除；剔完没有有效排序时回退到该接口的默认排序。所以「排序没生效」优先查字段权限，见 [权限模型](./backend/permission)。
:::

## 动态 API 路由推导规则

BasicApp 没有 Controller——应用服务打上 `[DynamicApi]` 就是 REST 接口（机制见[动态 API](https://framework.docs.xihanfun.com/guide/dynamic-api)）。对接时只需记住这套推导：

**路由 = `/api` + 控制器名 + 动作名**，控制器名取服务类名去掉 `AppService` / `QueryService` 等后缀，或由类级 `RouteTemplate` 直接钉死（如 `AuthAppService` 钉在 `api/Auth`）。

动作名 = 方法名**去掉 `Async` 后缀**再**剥离动词前缀**；HTTP 谓词由同一个动词前缀推导：

| 方法名前缀 | HTTP 谓词 | 方法名前缀 | HTTP 谓词 |
| --- | --- | --- | --- |
| `Get` / `Retrieve` / `Fetch` / `Find` / `Query` / `List` / `Search` | `GET` | `Update` / `Edit` / `Modify` | `PUT` |
| `Create` / `Add` / `Insert` | `POST` | `Delete` / `Remove` / `Destroy` | `DELETE` |
| `Patch` / `PartialUpdate` | `PATCH` | **无匹配前缀** | **`POST`（默认）** |

细节：

- **前缀匹配要求词边界**（前缀后必须是大写字母或下划线），所以 `AddressBook` 不会被 `Add` 命中、`EditorTemplate` 不会被 `Edit` 命中。
- 方法上显式标注的 `[HttpGet]` / `[HttpPost]` 等**优先于**前缀推导。
- 前缀被剥离：`GetUserPageAsync` → 动作名 `UserPage`；`SwitchTenantAsync`（无动词前缀）→ 动作名 `SwitchTenant`，谓词落默认 `POST`。

举例：

| C# 方法 | 实际端点 |
| --- | --- |
| `UserQueryService.GetUserDetailAsync(long id)` | `GET /api/UserQuery/UserDetail?id=...` |
| `UserQueryService.GetUserPageAsync(dto)` + `[HttpPost]` | `POST /api/UserQuery/UserPage` |
| `UserAppService.CreateUserAsync(dto)` | `POST /api/User/User` |
| `UserAppService.UpdateUserStatusAsync(dto)` | `PUT /api/User/UserStatus` |
| `AuthAppService.SwitchTenantAsync(dto)` | `POST /api/Auth/SwitchTenant` |

## 错误处理约定

- **HTTP 状态码与 `code` 通常一致**，但请以信封里的 `code` 为准做业务判定。
- `401` → 尝试刷新令牌并重放；再失败即登出。
- `423` → 弹解锁，**不要**登出。
- `429` → 退避重试。
- 校验失败（`400` / `422` / `11000`）时明细在 `data`，直接展示即可。
- 排障时把响应头 `X-Trace-Id`（或信封里的 `traceId`）交给后端，可在日志与审计里定位到这一次请求的完整轨迹。

## 开放接口：签名调用（无 JWT）

面向**服务端到服务端**的第三方调用，走 HMAC 签名而非 JWT。用户在[个人中心](./backend/authentication#个人中心)自助申请凭证：`AppKey`（`ak_` 前缀）+ `AppSecret`（`sk_` 前缀，**明文仅在创建/滚动时返回一次**，服务端只存哈希），每人最多 5 个。

由框架中间件 `XiHanOpenApiSecurityMiddleware` 统一验签，配置节 `XiHan:Web:Api:OpenApiSecurity`：

| 配置 | 默认值 | 说明 |
| --- | --- | --- |
| `IsEnabled` | `false` | 总开关 |
| `ProtectedPathPrefixes` | **`["/api"]`** | 受保护的路径前缀。框架默认值是 `/api`，**必须覆盖**，否则开启后整站接口都要验签；BasicApp 已在 `appsettings.json` 里改成 `["/api/openapi"]` |
| `IgnoredPathPrefixes` | — | 豁免前缀，BasicApp 配 `["/openapi", "/swagger", "/health", "/connect"]` |
| `AllowUnsignedRequests` | `false` | 灰度开关：允许未带安全头的请求直接放行（BasicApp 默认打开，便于灰度上线） |
| `RequireContentSignature` | `true` | 是否强制校验内容签名 |
| `EnableReplayProtection` | `true` | 防重放（Nonce 去重） |
| `TimestampToleranceSeconds` | `300` | 时间戳允许误差 |
| `NonceExpireSeconds` | `300` | Nonce 存活期 |
| `DefaultSignatureAlgorithm` | `HMACSHA256` | 也支持 `HMACSHA512` / `RSASHA256` / `SM2`；`HMACSHA1` 需显式放开 `AllowLegacySignatureAlgorithms` |
| `DefaultContentSignatureAlgorithm` | `SHA256` | 也支持 `SHA512`；`MD5` 需显式放开 |
| `MaxRequestBodySize` | `2 MiB` | 允许读取的最大请求体 |

### 请求头

| 请求头 | 说明 |
| --- | --- |
| `X-Access-Key` | 凭证 AppKey |
| `X-Timestamp` | Unix 秒 |
| `X-Nonce` | 一次性随机串（防重放） |
| `X-Signature` | 请求签名（Hex 或 Base64 均接受，HMAC 场景大小写不敏感） |
| `X-Content-Sign` | 请求体内容签名 |
| `X-Sign-Algorithm` / `X-Content-Sign-Algorithm` | 覆盖默认算法（可选） |
| `X-Encrypt-Algorithm` | 请求体加密算法；**不加密时必须显式发 `NONE`** |
| `X-Encrypt-Iv` | 请求体加密 IV（Base64，加密时用） |
| `X-Encrypt-Response` | 要求响应加密 |

### 签名怎么算

1. **内容签名**：`contentSign = lowerhex(SHA256(请求体原文))`，无 body 时对空串求。
2. **规范化查询串**：查询参数按 key 序（Ordinal）升序、同 key 值再升序，`Uri.EscapeDataString` 转义后以 `key=value` 用 `&` 连接；无查询参数则为空串。
3. **待签名串**：以 **`\n`** 连接六段，顺序固定——

   ```text
   {HTTP方法大写}\n{请求路径}\n{规范化查询串}\n{contentSign}\n{X-Timestamp}\n{X-Nonce}
   ```

4. **签名**：`X-Signature = HMACSHA256(AppSecret, 待签名串)`，输出 Hex 或 Base64 均可。

### 自测端点

Saas 模块内置两个只做回显的自测端点（`[AllowAnonymous]`，身份完全由签名确立），验签通过后回显调用方 AppKey，用来确认链路打通：

| 端点 | 方法 | 说明 |
| --- | --- | --- |
| `/api/openapi/Ping` | GET | 返回 `ok` / `accessKey` / `ownerUserId` / `serverTimeUtc` |
| `/api/openapi/Echo` | POST | 原样回显请求体 |

::: warning 两个高频坑
1. `ProtectedPathPrefixes` 别用框架默认的 `/api`——那会让登录、前端调用等全部接口都要求签名。只钉开放接口专属前缀。
2. 不加密请求体时**必须显式发 `X-Encrypt-Algorithm: NONE`**，否则会走加密解析分支而失败。
:::

## 实时通信（SignalR）

两条 Hub：通知 `/hubs/notification`、聊天 `/hubs/chat`。鉴权用同一个 JWT，经 `accessTokenFactory` 传递（浏览器 WebSocket 不能自定义头，SignalR 会把令牌放到查询串 `access_token`）。传输回退顺序 WebSockets → SSE → LongPolling。

::: warning 载荷需手动投影
Hub 侧**没有** `long → string` 与枚举的自动转换器（那是 MVC JSON 管道的配置，不作用于 SignalR）。服务端推送前须应用侧手动投影，Hub 方法参数用 `string` 接收 ID。
:::

## 下一步

- [常见问题](./faq)：401/403、菜单不出、时间差 8 小时等高频故障速查
- [身份与认证](./backend/authentication)：会话、2FA、OAuth、密码安全的完整设计
- [权限模型](./backend/permission)：权限码、数据范围、字段级脱敏（决定你能查到什么字段）
- [二次开发](./backend/development)：怎么新增一个接口
- [动态 API 概念](https://framework.docs.xihanfun.com/guide/dynamic-api)：框架层的路由推导实现
