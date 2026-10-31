# 配置参考

本页列出 6.0 宿主与管理端的配置项、缺省值与含义，适用于开发、部署与运维时查阅。

## 文件与优先级

宿主是标准 ASP.NET Core 应用，配置按 .NET 的缺省顺序叠加：`appsettings.json` → `appsettings.{环境}.json` → 用户机密（Development）→ 环境变量 → 命令行。环境变量用双下划线分节，如 `XiHan__Security__Jwt__Secret`。

配置分两组：

- 框架节 `XiHan:*`：由 XiHan.Framework 5 的各个 Feature 读取（内核、绑定、持久化、安全、租户、本地化、审计、协议）。
- 应用节 `BasicApp:*`：由各能力包的 Options 读取，直接绑定，不进入清单的配置贡献。

::: danger 不提交密钥
`appsettings.json` 里的 JWT 密钥、签名地址密钥、数据库连接串都留空；`appsettings.Development.json` 里的值只用于本机开发，部署前必须由环境变量或密钥存储替换。下文示例一律用占位符。
:::

## `Kestrel`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Kestrel:Endpoints:Http:Url` | `http://localhost:9710` | HTTP API、OpenAPI、MCP、Hub |
| `Kestrel:Endpoints:Grpc:Url` | `http://127.0.0.1:9711` | gRPC，`Protocols: Http2`，只应监听内网 |

## `XiHan:Bindings:db`

平台库连接，启动硬要求：缺失即 `RUNTIME_VALIDATION_001` 中断启动，不回退到内存实现。

```json
{
  "XiHan": {
    "Bindings": {
      "db": {
        "type": "connection:relational",
        "value": "Host=<主机>;Port=5432;Database=<库名>;Username=<用户>;Password=<密码>",
        "provider": "postgresql"
      }
    }
  }
}
```

独立库租户的连接串不写在配置里：由 `tenants.configure-database` 写进租户目录，以 `ISecretProtector` 密文存放。

## `XiHan:Persistence:SqlSugar`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `InitTables` | `false`（Development 为 `true`） | 启动时按实体建缺表、加缺列；只用于开发，正式环境由 `Schema/` 脚本管理 |

## `XiHan:Security`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `RoleClaim` | `role` | 令牌里的角色声明名 |
| `Jwt:Secret` | 空 | HS256 密钥，至少 32 字节；Identity 签发与框架校验共用，Hub 的 JwtBearer 也用它 |
| `Jwt:Issuer` / `Jwt:Audience` | `xihan.basicapp` | 签发者与受众 |
| `Jwt:ClockSkewSeconds` | `60` | 时钟容差 |

## `XiHan:ObjectStorage:SignedUrls:Secret`

本地存储签名地址（宿主 `/blobs` 端点）的签名密钥。S3 兼容存储由存储自己签名，不用它。

## `XiHan:MultiTenancy`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `RequireTenant` | `false` | 平台主体没有租户声明，必须为 `false` |
| `TenantClaim` | `tenant` | 令牌里的租户声明名 |

租户只从令牌声明（及框架 Baggage）解析，不支持查询串与 `X-Tenant-Id` 头。

## `XiHan:Localization:DefaultLocale`

缺省 `en-US`。请求按 `Accept-Language` 选区域，内置 `en-US`、`zh-CN`、`zh-TW`、`ja-JP`、`ko-KR`、`de-DE`、`es-ES`、`fr-FR`、`pt-BR`、`ru-RU`。

## `XiHan:Auditing`

| 键 | 值 | 说明 |
| --- | --- | --- |
| `Selection` | `None` | 框架审计中间件不自行挑选；由 Auditing 包的中间件按「是否标了审计」显式声明 |
| `IncludeSuccesses` | `true` | 成功的调用也记 |

## `XiHan:Protocols`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Http:RoutePrefix` | `/api` | 操作路由前缀 |
| `Grpc:Package` | `xihan.basicapp` | gRPC 服务名前缀（`/xihan.basicapp.Users/Get`） |
| `Mcp:ServerName` | `XiHan.BasicApp` | MCP 服务名 |

## `XiHan:Kernel`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `StartTimeoutSeconds` / `StopTimeoutSeconds` | `60` / `30` | 应用启停时限 |
| `FailurePolicy` | `TerminateStartup` | Feature 启动失败即中断 |
| `Resilience` | — | 按操作 ID 的熔断、固定窗口限流与并发上限，可按操作覆盖（框架 ADR-0029） |

## `BasicApp:ApiDocs`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Enabled` | 没写时 Development 为真、其余为假 | 是否映射 `/openapi.json`、`/openapi/<组>.json` 与 `/scalar` |

## `BasicApp:Schema`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `AutoUpgrade` | `true` | 启动时执行表结构升级，失败即中断启动 |
| `Directory` | 程序目录下的 `Schema` | 脚本目录 |
| `LockExpiryMinutes` / `LockWaitMinutes` | `30` / `10` | 升级租约锁的有效期与等待时长 |

## `BasicApp:Seed`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `EnableDemoData` | `false`（Development 为 `true`） | 是否写演示租户 `demo` 及其组织数据 |
| `SuperAdminPassword` | 空 = `SuperAdmin@123` | `superadmin` 的初始密码，部署前改掉 |

## `BasicApp:Modules`

可选模块开关 `BasicApp:Modules:<名>:Enabled`：没写即开启，写了必须是 `true` / `false`。目前只有 `Workbench`；关掉后前端把 `VITE_HOME_PATH` 留空。

## `BasicApp:Auth`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `AccessTokenMinutes` | `120` | 访问令牌有效期 |
| `RefreshTokenDays` | `7` | 刷新令牌（会话）有效期 |
| `MaxFailedAccessAttempts` / `LockoutMinutes` | `5` / `15` | 连续失败锁定 |
| `PasswordMinLength` / `PasswordHistoryDepth` | `8` / `3` | 密码长度与不可复用的历史深度 |
| `SessionCacheSeconds` | `30` | 会话门禁的缓存秒数 |
| `CaptchaEnabled` / `CaptchaSeconds` | `true` / `300` | 图形验证码 |
| `LoginThrottleWindowSeconds` | `60` | 登录节流窗口 |
| `LoginThrottleAccountLimit` / `LoginThrottleIpLimit` | `5` / `30` | 同账号同地址、同地址跨账号的失败上限 |
| `TwoFactorTicketMinutes` / `TwoFactorTicketMaxFailures` | `10` / `5` | 双因素票据 |
| `TotpIssuer` | `XiHan BasicApp` | 认证器里显示的签发方 |
| `VerificationCodeMinutes` / `VerificationCodeIntervalSeconds` | `10` / `60` | 邮箱 / 短信验证码有效期与发送间隔 |
| `VerificationCodeDailyPerTarget` / `VerificationCodeDailyPerAddress` | `10` / `50` | 每天每个目标、每个地址的发送上限 |
| `VerificationCodeMaxFailures` / `VerificationCodeLockoutMinutes` | `5` / `10` | 验证码输错锁定 |

登录页开放哪些方式不在这里，而是系统参数 `identity.login-methods`（`password`、`email`、`phone`），见[系统设置](./backend/settings)。

### `BasicApp:Auth:OAuth`（第三方登录）

```json
{
  "BasicApp": {
    "Auth": {
      "OAuth": {
        "Enabled": true,
        "FrontendCallbackUrl": "/#/auth/oauth-callback",
        "CallbackBaseUrl": "https://<后端对外地址>",
        "Providers": [
          { "Name": "github", "ClientId": "<客户端 ID>", "ClientSecret": "<客户端密钥>" },
          { "Name": "wecom", "ClientId": "<corpId>", "ClientSecret": "<secret>", "AgentId": "<agentId>", "Mode": "QrCode" }
        ]
      }
    }
  }
}
```

`Name` / `Provider` 支持 `github`、`gitee`、`google`、`qq`、`wechat`、`wecom`、`lark`（飞书）、`dingtalk`；没填 `ClientId` 或 `Enabled: false` 的提供方不出现在登录页。`Mode` 为 `QrCode`（缺省）或 `Account`，另有 `DisplayName`、`CorpId`、`Scopes`、`LoadMemberProfile`。

## `BasicApp:Access`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `PermissionConditionMode` | `Enforce` | 权限条件：`Enforce` / `LogOnly` / `Disabled` |
| `AccessPolicyEnabled` | `true` | 访问策略总开关 |
| `ConditionTimeZone` | `Asia/Shanghai` | 条件里时段、日期的判定时区 |

## `BasicApp:Auditing`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `QueueCapacity` / `BatchSize` / `FlushIntervalSeconds` | `10000` / `200` / `1` | 日志写入队列 |
| `AccessLog:Enabled` / `AccessLog:Exclude` | `true` / `[]` | 访问日志开关与排除的操作 |
| `DefaultRetentionDays` | `180` | 系统参数 `auditing.retention-days` 没建时的保留天数 |
| `RetentionCron` | `0 3 * * *` | 保留清理作业的计划（UTC） |

## `BasicApp:Files`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `LocalRootPath` | `App_Data/files` | 本地存储根目录（相对内容根） |
| `MaxUploadBytes` | 100 MB | 单文件上传上限 |
| `UploadPartBytes` | 8 MB | 分片大小（不小于存储要求的最小分片） |
| `UploadLifetime` | `1.00:00:00` | 分片上传会话有效期 |
| `LinkLifetime` / `MaxLinkLifetime` | `00:30:00` / `7.00:00:00` | 签名地址缺省与最长有效期 |

## `BasicApp:Notification`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Brand` | 应用名 | 模板变量 `brand` |
| `DispatchInterval` / `RetryDelay` / `MaxAttempts` / `StuckAfter` | `00:00:05` / `00:00:30` / `3` / `00:10:00` | 邮件 / 短信 / 机器人发件箱的轮询、重试与卡死判定 |
| `Telegram:*` | — | Telegram 交互：回复文案（`Texts`）、出站重试（`Retry`）、并发、去重时长、长轮询超时；机器人本身在后台页面配置 |

## `BasicApp:OpenPlatform`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `TimestampToleranceSeconds` | `300` | 签名请求的时间戳容差 |
| `MaxRequestBodyBytes` | 2 MB | 开放接口请求体上限 |
| `Issuer` | 请求来源 + `/api/oauth2` | OIDC 签发者，不带结尾斜杠；反向代理后面要显式配置 |
| `ConsentPageUrl` | `/#/oauth/authorize` | 前端授权同意页 |
| `IdTokenLifetimeSeconds` | `600` | ID 令牌有效期 |

## `BasicApp:DataProtection:KeysDirectory`

`ISecretProtector`（加密参数、存储密钥、租户连接串、认证器密钥）使用 ASP.NET Core Data Protection。多实例部署时必须配置共享的密钥目录，否则各实例互相解不开密文。

## 前端 `.env`

| 变量 | 缺省 | 说明 |
| --- | --- | --- |
| `VITE_PORT` | `9100` | 开发服务器端口 |
| `VITE_DEV_PROXY_TARGET` | `http://localhost:9710` | 开发代理目标 |
| `VITE_API_BASE_URL` | 空（同源） | 构建期 API 源 |
| `VITE_HOME_PATH` | `/workbench/dashboard` | 首页；关掉工作台模块时留空 |
| `VITE_ROUTER_HISTORY` | `hash` | `history` 需要静态托管回退到 `index.html` |
| `VITE_BASE` | `/` | 部署子路径 |
| `VITE_REALTIME_HUB` | `/hubs/notification` | 留空即不建实时连接 |
| `VITE_APP_TITLE` 等 | 空 | 登录页品牌区 |

本机改动写 `.env.local`，不提交。

## 下一步

- [部署](./deployment) · [数据库配置](./backend/database) · [统一认证](./backend/authentication)
