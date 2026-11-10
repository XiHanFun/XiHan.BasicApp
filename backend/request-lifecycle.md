来源：https://basicapp.docs.xihanfun.com/backend/request-lifecycle

# 请求生命周期

本页说明一次调用从进入宿主到返回响应所经过的各个阶段，以及 BasicApp 在各阶段贡献的中间件，适用于排查认证、权限与审计问题的后端开发者。

一个 HTTP 请求先经过宿主的 ASP.NET Core 中间件，再进入 XiHan 内核管道；内核管道分 11 个阶段，每个阶段里有框架内置与各能力包贡献的中间件。gRPC、MCP 与 CloudEvents 的调用从各自的协议适配器进入同一条内核管道，所以租户、权限、事务、审计的行为各协议一致。

## 一、ASP.NET Core 段

`BasicAppHost.Map` 的顺序：

| 顺序 | 中间件 | 作用 |
| --- | --- | --- |
| 1 | `UseForwardedHeaders` | 还原代理转发的客户端地址、协议与主机（`BasicApp:ForwardedHeaders`，缺省只信本机回环）；之后的日志、会话、节流与访问策略拿到的都是还原后的地址 |
| 2 | `ResponseSecurityHeadersMiddleware` | 一律加 `X-Content-Type-Options: nosniff` |
| 3 | `UseCors` | 只在 `BasicApp:Cors` 配了来源时启用；维护期间的 503 也带跨域头 |
| 4 | `MaintenanceModeMiddleware` | 别的节点升级共享库期间返回 503（`RUNTIME_UNAVAILABLE_003`，带 `Retry-After`）；放行 `/health` 与 `/.well-known/` |
| 5 | `UseAuthentication` / `UseAuthorization` | 只服务绕过内核管道的端点（实时 Hub）：XiHan 方案按 `Security.Jwt` 的同一套参数校验，令牌只在 `/hubs` 下从查询串读 |
| 6 | `UseOpenApiRequestBuffer` | 为签名开放接口留下请求体原文（验签要原文，加密的要先解密） |
| 7 | `UseXiHan()` | 进入内核管道 |

绕过内核管道的端点（`BypassXiHanPipeline()`）：Scalar 页面、根路径自述、实时 Hub、OAuth 2.0 / OIDC 端点、第三方登录的跳转与回调、Telegram Webhook。它们没有调用上下文，自己处理认证与错误。

## 二、内核管道

```text
Ingress → Observe → Resilience → Authenticate → Tenant → Authorize → Validate → Idempotency → Transaction → Handle → Egress
```

| 阶段 | 框架内置 | BasicApp 贡献 |
| --- | --- | --- |
| Ingress | 协议归一化、大小限制、构造调用上下文与调用来源（协议、目标、远端地址、User-Agent） | — |
| Observe | 链路、日志、指标；Full Profile 的 `AuditMiddleware`（`XiHan:Auditing:Selection = None`，由 Auditing 显式声明要审计的操作） | `AuditLogMiddleware`（Auditing，排在框架审计之内）：标了 `audited` 的操作声明审计动作并补耗时、登录名、会话、客户端信息；按结果写访问、异常日志，以及调用里登记的登录、开放接口、条件命中日志，权限变更日志与数据变更只在成功时写 |
| Resilience | 截止时间（`xihan-deadline`）、按操作 ID 的限流、并发上限与熔断（`XiHan:Kernel:Resilience`） | — |
| Authenticate | JWT 校验（HS256） | `SessionGateMiddleware`（Identity，排在 JWT 之后）：令牌带 `sid` 时核对会话有效，已吊销、已过期或用户不正常 → `IDENTITY_DENIED_001`；锁定的会话只放行 `auth.unlock` 与 `auth.logout`（强制改密锁另放行 `profile.change-password`），其余 `IDENTITY_DENIED_002`<br>`RequestSigningMiddleware`（OpenPlatform，只对 `open.*`）：验签、防重放、解密，并登记开放接口日志 |
| Tenant | 从令牌 `tenant` 声明（及内部调用的 Baggage）解析租户 | `AccessPolicyMiddleware`（Access，排在租户解析之后，只管 HTTP、gRPC、MCP 入站）：先看登录失败自动封禁（503 `ACCESS_UNAVAILABLE_001`），再求平台层、租户层访问策略，拒绝 → 403 `ACCESS_DENIED_004`，命中都记条件命中；判定本身出错时放行并记警告 |
| Authorize | 按操作声明的权限码 / 策略判定；`PermissionChecker` 查授予存储与 `IPermissionGrantSource`，再经各 `IPermissionFilter` 收窄 | Access 登记的 `AccessGrantSource`（继承、委托）、`AccessPermissionFilter`（Deny、有效期、作用侧）、`EditionPermissionFilter`（套餐门控）<br>`PermissionConditionMiddleware`（排在框架授权之后）：持有的权限码挂了条件时按请求属性精确判定 |
| Validate | 按操作请求的 JSON Schema 校验载荷，再执行 `IValidatableObject`；失败带字段级 `errors` | — |
| Idempotency | 幂等键去重（`XiHan:Caching:Idempotency`） | — |
| Transaction | 工作单元：成功提交、失败回滚；提交前把领域事件写进发件箱 | 阶段开头、工作单元之外：`CacheInvalidationMiddleware`（Runtime，调用里记下的缓存失效在提交或回滚之后才删键）、`RealtimeDispatchMiddleware`（Realtime，调用里的推送先暂存，提交且成功后才发出）、`PublishProgressMiddleware`（Notification，通知发布的开始与失败立即推给发布人）、`AiProviderCatalogMiddleware`（AI，提供商写入提交后重载目录）<br>`AuditorStampMiddleware`（Runtime）：把当前主体 ID 放进异步本地值，SqlSugar 回调据此填 `CreatedBy` / `ModifiedBy` / `DeletedBy` |
| Handle | 调用操作的 `HandleAsync` | `FieldSecurityMiddleware`（Access，工作单元之内）：对绑定了字段安全的操作，受保护的字段不能筛选、排序（`ACCESS_DENIED_006`），只读字段不能填、不能改（`ACCESS_DENIED_007`）；成功响应编码前再由响应过滤器打码。数据范围在各包的查询处显式套用 |
| Egress | 结果与错误翻译：错误码 → 状态码，消息键按调用方区域翻译成 `detail` | — |

用 `dotnet run --project backend/src/XiHan.BasicApp.Host -- pipeline` 可以打印实际的中间件次序。同阶段的中间件按清单 ID 排序，跨包的先后由 `context.Pipeline.After<,>()` / `Before<,>()` 显式声明。

## 三、典型路径

### 未认证与会话失效

| 情况 | 阶段 | 错误码 | 客户端动作 |
| --- | --- | --- | --- |
| 未携带令牌 | Authorize | `SECURITY_DENIED_001`（401） | 刷新一次，失败则返回登录页 |
| 令牌过期 / 签名不符 | Authenticate | `SECURITY_DENIED_003`（401） | 刷新一次并重放 |
| 会话被吊销 | Authenticate（会话门禁） | `IDENTITY_DENIED_001` | 返回登录页 |
| 已锁定 | Authenticate（会话门禁） | `IDENTITY_DENIED_002` | 按 `details.reason` 显示锁屏或强制改密 |
| 没有权限码 / 被 Deny / 套餐不含 | Authorize | `SECURITY_DENIED_004`（403） | 提示无权限 |
| 共享库升级中 | ASP.NET Core（维护模式） | `RUNTIME_UNAVAILABLE_003`（503） | 提示稍后重试 |

### 失败调用的副作用

调用失败时，事务回滚，仓储写入随之撤销；调用中暂存的实时推送与缓存失效按回滚处理（推送丢弃，缓存失效照常执行）。访问日志与异常日志照常记录，标注了审计的操作在操作日志中记录失败；权限变更日志与数据变更只在调用成功时记录。登录相关的事件（密码错误、验证码错误、节流拒绝、第二步验证码错误）无论成败都记入登录日志。

### 日志写入

所有日志只进有界队列（`BasicApp:Auditing:QueueCapacity`），由后台批量落库，写日志不改变调用结果。

## 四、普通端点

上传、下载、头像等普通端点不属于操作，但仍通过 `RequireXiHanPermission(...)` / `RequireXiHanAuthentication()` 经过内核的认证与授权；宿主的端点过滤器在端点执行时登记端点身份与实际状态码，访问日志与数据变更按 `files.upload` 这类名字记录，写入类端点另进操作日志。

## 下一步

- [统一认证](./authentication) · [权限管理](./permission) · [访问策略与权限条件](./access-policy) · [日志审计](./logging)
