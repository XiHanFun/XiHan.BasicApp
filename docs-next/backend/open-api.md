# 开放平台

本页说明 OpenPlatform 包（Tier 2）提供的两种对外接入方式，适用于后端开发者与第三方对接方。

| 方式 | 适合 | 凭据 |
| --- | --- | --- |
| 签名开放接口 | 服务端到服务端，调用 `open.*` 操作 | 个人中心申请的 AppKey / 密钥，HMAC 签名，可加密请求与响应 |
| OAuth 2.0 / OIDC 服务端 | 第三方应用以用户身份登录、取用户信息 | OAuth 应用的客户端 ID / 密钥，授权码 + PKCE |

本页的「开放接口」与接口文档的 `/openapi.json`（OpenAPI 规范文档）不是一回事。

## 签名开放接口

### 凭证

开放接口凭证是 Identity 的个人中心自助管理的（`SysUserApiCredential`，密钥加密存储）：

| 操作 | 说明 |
| --- | --- |
| `my-api-credentials.list` / `create` / `update` / `delete` | 申请与维护凭证 |
| `my-api-credentials.rotate-secret` | 轮换密钥 |
| `my-api-credentials.settings` / `update-settings` | 签名算法（`HmacSHA256` / `HmacSHA512`）与 IP 白名单，作用于本人全部凭证 |

凭证停用、或归属人账号不正常时验签一律失败；验签通过即记下凭证的最近使用时间。

### 验签中间件

`RequestSigningMiddleware` 在 Authenticate 阶段、只对 `open.*` 操作生效，依次校验：请求头 → 时间窗（`BasicApp:OpenPlatform:TimestampToleranceSeconds`，300 秒）→ 凭证 → IP 白名单 → 算法 → 请求体（超限 `MaxRequestBodyBytes`、解密）→ 内容签名 → 请求签名，最后才占用随机串防重放（伪造的请求不能把正常请求的随机串占掉）。签名用恒定时间比较；失败按类别给 400 / 403 / 409。

请求头、规范串与加密方式沿用 5.x，接入方无需修改，详见[接口对接指南](../api-guide)。与 5.x 的不同：

- 明文请求不必带 `X-Encrypt-Algorithm: NONE`；
- 算法以凭证的设置为准，请求头只能与之一致；
- 不再有「允许不签名」的开关。

宿主在 `UseXiHan()` 之前经 `UseOpenApiRequestBuffer` 留下请求体原文，验签用原文、加密的先解密再重新绑定载荷。

### 开放操作

| 操作 | 说明 |
| --- | --- |
| `open.ping` | 验签自检：返回 AppKey、归属人与服务端时间 |
| `open.echo` | 回显（解密后的）请求，核对请求体、内容签名与加密 |

验签中间件只对 `open.*` 操作生效，需要开放给第三方的能力以 `open.*` 操作的形式新增。

### 开放接口日志

每次 `open.*` 调用无论成败都登记一条开放接口日志（凭证、结果、耗时、请求响应摘要），由 Auditing 落库，`open-api-logs.*` 查询（`auditing.open-api-logs.read`）。

## OAuth 2.0 / OIDC 服务端

### 端点

前缀 `/api/oauth2`，宿主映射的普通端点，绕过内核管道，回原始 OAuth JSON（不是 Problem Details）：

| 端点 | 说明 |
| --- | --- |
| `GET /api/oauth2/authorize` | 把查询串原样带去前端同意页（`BasicApp:OpenPlatform:ConsentPageUrl`，缺省 `/#/oauth/authorize`） |
| `POST /api/oauth2/token` | 表单：`authorization_code` + PKCE、`refresh_token` |
| `POST /api/oauth2/revoke` | RFC 7009 |
| `GET` / `POST /api/oauth2/userinfo` | 用户信息 |
| `GET /api/oauth2/.well-known/openid-configuration` | OIDC 发现文档 |
| `GET /api/oauth2/jwks` | 签名公钥（`SysOAuthSigningKey`） |

签发者取 `BasicApp:OpenPlatform:Issuer`，不配即请求来源加 `/api/oauth2`；反向代理后必须显式配置。ID 令牌有效期 `IdTokenLifetimeSeconds`（600 秒）。

### 同意

前端同意页（`packages/views/_core/oauth/authorize.vue`）用当前登录的用户调用：

| 操作 | 说明 |
| --- | --- |
| `oauth-consents.resolve` | 解析授权请求，展示应用与请求的范围 |
| `oauth-consents.approve` / `deny` | 同意或拒绝，同意即签发授权码并重定向 |
| `oauth-consents.list` / `revoke` | 个人中心「已授权的应用」 |

### 应用管理

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `oauth-apps.list` / `get` | `openplatform.oauth-apps.read` | 平台管理全部应用 |
| `oauth-apps.create` / `update` / `delete` | `openplatform.oauth-apps.create` / `update` / `delete` | 类型 `Web` / `Mobile` / `Desktop` / `Service`，客户端 `Confidential` / `Public`，回调地址、范围 |
| `oauth-apps.reset-secret` | `openplatform.oauth-apps.update` | 重置客户端密钥 |
| `my-oauth-apps.*` | 登录即可 | 个人中心「我的应用」：用户自助注册的应用 |

作业 `openplatform.oauth-cleanup` 每天 03:30（UTC）物理删除过期一天的授权码与过期七天的令牌。

## 第三方登录

用 GitHub、企业微信这类外部身份登录 BasicApp，是 Identity 的能力，见[统一认证](./authentication)。

## 下一步

- [接口对接指南](../api-guide) · [统一认证](./authentication) · [日志审计](./logging)
