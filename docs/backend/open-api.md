# 开放能力

BasicApp 对外开放有**三条互不相同的路径**，最容易混淆的是前两条方向正好相反：

| 能力 | 方向 | 用途 |
| --- | --- | --- |
| **OAuth2 / OIDC 服务端** | 别人来接我 | 本平台作为 SSO 提供方，第三方应用「用 XiHan 账号登录」 |
| **第三方 OAuth 登录** | 我去接别人 | 登录页上的 GitHub / Gitee / Google / QQ 按钮 |
| **开放接口（签名调用）** | 服务端到服务端 | 第三方系统用 AppKey/AppSecret 签名调本平台接口，无 JWT |

第二条属于登录方式，见 [身份与认证 · 第三方登录](./authentication#第三方登录-oauth2)。本页讲第一、三条。

## OAuth2 / OIDC 服务端

平台自己当授权服务器，让别的应用接入。

### 应用注册（`/openapi/app`）

`SysOAuthApp`：

| 字段 | 说明 | 默认 |
| --- | --- | --- |
| `AppName` / `AppDescription` / `Logo` / `Homepage` | 展示信息（同意页上给用户看） | — |
| `ClientId` / `ClientSecret` | 客户端凭据 | — |
| `AppType` | `Web`(0) / `Mobile`(1) / `Desktop`(2) | `Web` |
| `GrantTypes` | 允许的授权类型 | — |
| `RedirectUris` | 允许的回跳地址（**白名单**） | — |
| `Scopes` | 可申请的授权范围 | — |
| `AccessTokenLifetime` | 访问令牌有效期（秒） | `3600` |
| `RefreshTokenLifetime` | 刷新令牌有效期（秒） | `2592000`（30 天） |
| `AuthorizationCodeLifetime` | 授权码有效期（秒） | `300` |
| `SkipConsent` | 跳过同意页（仅限自家可信应用） | `false` |

配套两张表：`Sys_OAuth_Code`（授权码）与 `Sys_OAuth_Token`（签发的令牌，支持吊销）。

### 标准端点

落在 `/connect/*`（RFC 标准路径），**都是匿名端点**：

| 端点 | 方法 | 说明 |
| --- | --- | --- |
| `/connect/authorize` | GET | 浏览器入口，**仅 302 跳转到已登录 SPA 的同意页** |
| `/connect/token` | POST | 表单编码，用授权码/刷新令牌换令牌 |
| `/connect/revoke` | POST | 表单编码，吊销令牌（RFC 7009） |

::: tip 为什么 `/connect/authorize` 只做跳转
第一方前端用的是 **JWT bearer，没有会话 Cookie**，所以后端在这个匿名端点上**无法判定用户是否已登录**。因此它不渲染同意页，只把请求参数带着 302 到 SPA 的同意页（`/oauth/authorize`），由已登录的前端去完成确认并调后端换码。
:::

### 一个实现细节

`/connect/token` 直接注入**普通 Scoped 的 `IOAuthServerService`**，不跨代理接口边界。这是为了避开一个已知陷阱：**匿名端点没有工作单元中间件，走 Castle 代理会让拦截器急切开事务从而死锁**。

同样的原因，第三方登录回调里的 `ExternalLoginAsync` 被标 `[DynamicApi(IsEnabled = false)]` 不对外暴露，且调用时用 `ProxyHelper.UnProxy` 取真实目标实例。

自己写匿名端点时记住这条，见 [框架常见问题](https://framework.docs.xihanfun.com/guide/faq#匿名端点调用应用服务时永久挂起)。

## 开放接口：签名调用

面向**服务端到服务端**的调用，身份由 HMAC 签名确立，**不需要 JWT**。

### 凭证从哪来

用户在[个人中心 · 开发者](./authentication#个人中心)自助申请：

- `AppKey`（`ak_` 前缀，全局唯一）
- `AppSecret`（`sk_` 前缀，**与账号密码同栈只存哈希，明文仅在创建/滚动时返回一次**）

每人最多 5 个，可随时滚动密钥或启停删除，变更均触发安全通知。实体是 `SysUserApiCredential`。

::: danger AppSecret 只显示一次
创建或滚动时返回的明文**不会再次提供**——服务端只存哈希。当场存进你的密钥管理系统，丢了只能滚动重发。
:::

### 怎么调

由框架中间件 `XiHanOpenApiSecurityMiddleware` 统一验签，只对 `ProtectedPathPrefixes` 命中的路径生效（BasicApp 配的是 `/api/openapi`）。

请求头、待签名串的构造、算法选择、两个自测端点（`/api/openapi/Ping`、`/api/openapi/Echo`）见 [接口对接指南 · 开放接口](../api-guide#开放接口-签名调用-无-jwt)——那里有可直接照抄的完整规则。

### 两个必踩的坑

::: warning
1. **`ProtectedPathPrefixes` 别用框架默认的 `/api`**。框架默认值就是 `/api`，一旦开启 `IsEnabled` 而没改这项，登录、前端调用等全部接口都会要求签名。BasicApp 已在 `appsettings.json` 改成 `["/api/openapi"]`。
2. **不加密请求体时必须显式发 `X-Encrypt-Algorithm: NONE`**，否则会走加密解析分支而失败。
:::

### 调用日志

开放接口的调用落 `Sys_OpenApi_Log`（按月分表），页面在 `/log/api`。排查第三方对接问题先看这里：能不能看到请求 → 验签有没有过 → 业务返回了什么。

## 三条路径的选型

| 场景 | 选哪条 |
| --- | --- |
| 第三方应用想让用户「用 XiHan 账号登录」 | OAuth2 服务端 |
| 我们的登录页想加「GitHub 登录」 | 第三方 OAuth 登录 |
| 合作方的后台系统要批量拉数据 | 开放接口签名调用 |
| 我们自己的前端调后端 | 都不用——直接 JWT |

## 相关页面

- [接口对接指南](../api-guide)：签名算法与自测端点
- [身份与认证](./authentication)：第三方登录、个人中心凭证管理
- [审计日志](./logging)：开放接口日志
- [数据模型](./data-model#开放能力与审批)：相关表结构
