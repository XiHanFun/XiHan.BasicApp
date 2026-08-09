# 身份与认证

XiHan.BasicApp 的身份体系围绕**统一身份**设计：一个自然人对应一个 `SysUser` 账号，用**邮箱**作为全平台唯一登录标识；同一账号可通过成员关系在多个租户里办事。本页讲清楚**主体模型**（用户/角色/部门/菜单/岗位）、**登录方式**、**令牌与会话**、**密码安全**、**登录落点**与**个人中心**。

> 权限如何判定（权限码、数据范围、字段脱敏、ABAC）见 [权限模型](./permission)；租户隔离、成员关系、版本门控见 [多租户](./multi-tenancy)。本页只做交叉链接，不重复。

## 主体模型

RBAC 的核心实体都落在 `Saas` 模块的 `Domain/Entities` 下，均为 `snake_case` 表、软删、带 `TenantId`：

| 实体 | 表 | 职责 |
| --- | --- | --- |
| `SysUser` | `Sys_User` | 身份主体（Subject）：用户名/邮箱/手机/资料。`Email` 全平台唯一（`UX_Em`），是登录标识 |
| `SysUserSecurity` | `Sys_User_Security` | 与 `SysUser` 一对一的**安全扩展**：密码哈希、锁定、MFA、失败计数、多端策略 |
| `SysRole` | `Sys_Role` | 角色：一组权限的分配单元，支持层级继承与数据范围 |
| `SysDepartment` | `Sys_Department` | 组织架构树节点（单父严格树），是数据范围 `DepartmentOnly` / `DepartmentAndChildren` 的基础 |
| `SysDepartmentHierarchy` | `Sys_Department_Hierarchy` | 部门层级**闭包表**：预计算祖先-后代对，支持 O(1) 展开 |
| `SysMenu` | `Sys_Menu` | 纯 UI 结构（导航/路由/展示），通过 `PermissionId` 绑定一个权限点 |
| `SysPosition` | `Sys_Position` | 扁平岗位字典（职务/职位），供人员任用引用 |
| `SysExternalLogin` | `Sys_External_Login` | 本地用户与第三方身份（github/google/qq…）的绑定关系 |
| `SysUserSession` | `Sys_User_Session` | 会话中心：登录状态、多端控制、设备信息、撤销 |
| `SysLoginLog` | `Sys_Login_Log_{按月分表}` | 登录/认证审计（含成功与失败），按月分表 |

关系（关联表）：

- 用户 ↔ 角色：`SysUserRole`（`Sys_User_Role`），多对多、支持带生效期的临时授权（`EffectiveTime`/`ExpirationTime`）。
- 用户 ↔ 部门：`SysUserDepartment`（`Sys_User_Department`），多部门归属，`IsMain` 标识主部门，可挂 `PositionId`（岗位）、工号、职级、入职日期。
- 角色 ↔ 权限、角色继承、数据范围等归权限模型，见 [权限模型](./permission)。

### 用户（SysUser）

一条重要语义澄清：`SysUser.TenantId` 是用户的**主账号归属租户**（注册地），**不等同于**"能进入哪个租户"——后者由多租户的成员关系（`SysTenantUser`）承载。因此：

- `TenantId + UserName` 在主归属租户内唯一；`Email` **全平台唯一**（可空，有值必唯一）。
- `Status`（启用/禁用）与 `IsActive`（是否激活，邮箱/手机验证）**正交**：未激活或被禁用都不可登录。
- `IsSystemAccount=true` 的内置账号禁止改用户名、禁止软删。
- 平台账号约定 `TenantId=0`（如超管），恒落平台运维态。
- 用户级数据范围可用 `DataScopeOverride` 覆盖角色默认（细节见权限模型）。

敏感安全字段刻意拆到一对一的 `SysUserSecurity`（`Password`/`TwoFactorSecret`/`SecurityStamp` 均 `[JsonIgnore]`，不出接口），避免污染用户主表、便于单独脱敏与访问控制。

### 部门与闭包表

部门是严格单父树。为了高效回答"某部门的全部下级/祖先"（数据范围 `DepartmentAndChildren` 要用），系统用**闭包表** `SysDepartmentHierarchy` 把所有"祖先-后代对"预计算出来（含 `Depth=0` 的自环），并冗余 `Path`/`PathName` 便于面包屑。

闭包表是"查询加速镜像"，不是独立业务数据——所有写入必须由 `SysDepartment` 变更触发，服务层在**增/删/移**部门时统一重建：

```text
新增部门 N 挂在父 P 下：
  INSERT SELECT AncestorId, N, Depth+1 FROM 闭包 WHERE DescendantId = P
  再补 (N, N, 0)
移动子树：先删旧闭包，再按新父重建
删除部门 N：DELETE WHERE DescendantId = N OR AncestorId = N
```

### 菜单

`SysMenu` 只描述 UI 层级（目录/菜单/按钮/外链），**后端鉴权永远基于 Permission，不依赖菜单存在性**。菜单通过可空的 `PermissionId` 反向绑定一个权限点：空=纯展示菜单；有值=按该权限码门控可见性。若一个菜单需要多个权限，约定**建组合权限点**再绑定，而非引多对多。

> 运行时的菜单/路由/组件路径/权限码/国际化键由后端 `PageRegistry` 作为单一事实源统一注册（见 [框架简介](./introduction)）；`SysMenu` 表是其落库形态。`TenantId=0` 为平台全局菜单，所有租户共享读取，租户可叠加私有菜单。

## 登录方式

登录入口是 `AuthAppService`（`[DynamicApi(RouteTemplate = "api/Auth")]`）。核心设计是**先登录后选租户**：登录页不选租户，统一在**平台态**（`TenantId=null`）完成身份认证，认证通过后再按成员关系决定落点（见下文"登录落点"）。

前端先调 `GET /api/Auth/LoginConfig` 拿到 `LoginConfigDto`（`loginMethods` + `oAuthProviders`），据此决定展示哪些登录按钮——**支持的方式以该配置为准**。

> 路由是方法名 `GetLoginConfigAsync` **剥离动词前缀 `Get` 并去掉 `Async`** 后的结果。全部认证端点的方法与 URL 对照表见 [接口对接指南](../api-guide#端点总表)。

### 账号密码

`POST /api/Auth/Login`（`LoginRequestDto`）。`Username` 传邮箱（全平台唯一定位）；平台账号也可用用户名。流程：

1. 平台态定位账号 → `IAuthenticationDomainService.AuthenticatePasswordLoginAsync`：校验存在性、`IsActive`、密码哈希、账户锁定/失败计数，并判定是否需要二次验证。
2. 同一域服务内的账号可用性校验：`Status` 是否启用、（带租户时）成员关系与生效/过期、密码是否过期。
3. 若 `RequiresTwoFactor` → 返回 2FA 挑战（不签发令牌，见下）。
4. 通过 → 决定落点租户、签发双令牌、落地会话、发布登录成功领域事件。

失败会发布 `AuthLoginFailedDomainEvent`（落 `SysLoginLog`），并抛出友好错误。

### 邮箱验证码

两步，均匿名：

1. `POST /api/Auth/EmailLoginCode`（`EmailLoginCodeRequestDto`）：邮箱+IP 频率限制（60s 窗口）→ 复用邮箱登录的账号可用性校验 → 下发验证码。
2. `POST /api/Auth/EmailLogin`（`EmailLoginRequestDto`）：一次性消费验证码 → 校验账号 → 签发令牌并落点。

验证码为 **6 位数字、有效期 10 分钟（600s）、一次性消费**（读取即销毁，无论成败），由框架 `IOneTimeCodeService` 存分布式缓存承载。开发环境会在结果里回显 `DebugCode` 便于联调，生产绝不回显。

### 第三方登录（OAuth2）

内建支持 **GitHub / Google / Gitee / QQ**，由框架 `XiHan.Framework.Authentication.OAuth` 提供，配置节 `XiHan:Authentication:OAuth`（`OAuthOptions`：`Enabled`/`FrontendCallbackUrl`/`Providers[]`）。前端登录页展示哪些提供商由运行时配置 `saas.auth.oauth.providers`（存库）决定。

OAuth 走**独立的 Web 端点**（不是动态 API），落在 `Infrastructure/OAuth/OAuthEndpoints.cs`：

```text
① 前端跳转：GET /api/OAuth/ExternalLogin?provider=github[&bindTicket=xxx]
      端点校验 provider（绑定意图则一次性消费 bindTicket 得到 userId）
      → ChallengeAsync(provider) → 302 到三方授权页
② 用户在三方授权
③ 三方回跳：GET /api/OAuth/Callback?code=...&state=...
      端点 AuthenticateAsync(ExternalCookie) 读出三方 Claim：
        NameIdentifier → providerKey、Email、Name → displayName、统一头像 Claim → avatar
      → 调 IAuthAppService.ExternalLoginAsync(...)   （见下方"注意"）
      → 302 回 FrontendCallbackUrl，成功时带 accessToken/refreshToken/expiresIn
```

`ExternalLoginAsync` 被标 `[DynamicApi(IsEnabled = false)]`（不对外暴露），只由回调端点内部调用；且调用时用 `ProxyHelper.UnProxy` 取真实目标实例——因为匿名端点没有 UoW 中间件，走 Castle 代理会让拦截器急切开事务而死锁。

登录/建号规则（`LoginByExternalAsync`）：

- 按 `(Provider, ProviderKey, TenantId)` **精确**定位用户（唯一约束 `UX_Pr_PrKe_TeId`）。
- 未绑定则**首登自动建号**：用户名 `{provider}_{随机hex}`、随机强密码、不分配角色；三方邮箱**仅在未被占用时**采用，否则用占位邮箱 `{username}@external.local`——**绝不按邮箱并入既有账号**（防冒用）。
- `SysExternalLogin` 只存身份关联（Provider/ProviderKey/邮箱/头像），**不存三方 Access/Refresh Token**。

**账号绑定**（把三方账号绑到已登录用户）：已登录用户先 `POST /api/Auth/OAuthBindTicket`（方法 `CreateOAuthBindTicketAsync`，动词前缀 `Create` 被剥离）拿一次性票据（`oauth:bind-ticket:{ticket}`，缓存 5 分钟），带 `bindTicket` 走同一发起端点；回调时以 `IsBind=true` 走 `BindExternalLoginAsync`，已被他人绑定则拒绝（`conflict`）。

> 具体支持哪些 provider、scope、回调地址以仓库配置为准。

### 两步验证（2FA / MFA）

`SysUserSecurity.TwoFactorMethod` 是 `[Flags]` 枚举，可同时启用多种：

| 方式 | 值 | 说明 |
| --- | --- | --- |
| `None` | 0 | 未启用 |
| `Totp` | 1 | 认证器 App（Google/Microsoft Authenticator 等） |
| `Email` | 2 | 邮箱验证码 |
| `Phone` | 4 | 短信验证码（要求已绑定且已验证手机号，否则不对外公布） |

当密码/邮箱登录判定 `RequiresTwoFactor`：

1. 未提交验证码 → 返回 `LoginResponseDto { RequiresTwoFactor=true, AvailableTwoFactorMethods, TwoFactorMethod, CodeSent }`；对 email/phone 方式会**先下发验证码**（TOTP 由认证器本地生成，无需下发）。
2. 提交验证码 → 按方式校验：`totp` 走 `IOtpService.VerifyTotpCode`；`email` 走一次性验证码消费；`phone` 走个人中心验证服务消费。校验失败发布失败事件并抛错。
3. 通过 → 继续签发令牌。

TOTP 遵循 RFC 6238：HMAC-SHA1、Base32 密钥、6 位、30 秒步长、±1 窗口容差；provisioning URI 形如 `otpauth://totp/{issuer}:{account}?secret=...&period=30&digits=6`。用户在[个人中心](#个人中心)开启/关闭各方式。

## 令牌与会话

### JWT 双令牌

登录成功签发 `LoginTokenDto`：**Access Token**（JWT，HMAC-SHA256 签名）+ **Refresh Token**（不透明随机串）。配置节 `XiHan:Authentication:Jwt`（`JwtOptions`），默认 Access 60 分钟、Refresh 7 天、时钟偏移容差 5 分钟（**以仓库配置为准**）。

Access Token 的 Claim 主要有：`sub`/`jti`、`UserId`、`UserName`、`SessionId`、`TenantId`（有则带）、`Email`/`Phone`/`Picture`、`DeviceFingerprint`、多个 `Role`。**具体权限码不冻结进令牌**——只在超管时放一个通配 `*` 作快路径；其余权限一律由服务端**实时快照**判定，避免授予/回收后令牌失效不及时，也避免权限清单泄露（与 [权限模型](./permission) 的实时校验一致）。

刷新：`POST /api/Auth/RefreshToken`（`RefreshTokenRequestDto` 带旧 AccessToken + RefreshToken）→ 校验后签发新令牌，并落一条 `TokenRefreshed` 审计。

### 会话与多端登录

每次登录写一条 `SysUserSession`（会话中心）：记录 `UserSessionId`（业务会话号）、`CurrentAccessTokenJti`（只存 JTI 不存完整 Token，便于黑名单/撤销）、设备类型/名称/指纹、浏览器/OS/IP/位置、登录与最后活动时间、`ExpirationTime`。会话状态 `SessionStatus`：

| 状态 | 值 | 含义 |
| --- | --- | --- |
| `Active` | 0 | 活跃（鉴权"会话有效"= `Active` 且未软删） |
| `Offline` | 1 | 离线（未被强制撤销，可按策略重新激活） |
| `Revoked` | 2 | 登出/强制下线/安全撤销 |
| `Expired` | 3 | 超过绝对过期时间 |

多端策略由 `SysUserSecurity` 控制：`AllowMultiLogin`（是否允许多端，默认 true）与 `MaxLoginDevices`（最大设备数，0=不限）。切换租户复用**同一条会话记录**：会话行的 `TenantId` 迁移到目标上下文（平台态戳 `0`），不新建会话。

登出 `POST /api/Auth/Logout`：按 `userId + SessionId` 撤销会话、吊销关联令牌，并发布登出领域事件。用户自助的"我的设备/会话"管理、踢其他端，见[个人中心](#个人中心)；管理员侧撤销走 `UserSessionAppService`（需 `SaasPermissionCodes.UserSession.Revoke` 权限）。删除用户时会吊销其全部会话并实时 `ForceLogout` 踢出在线连接。

## 密码安全

- **哈希算法：PBKDF2**（框架 `XiHan.Framework.Security.Password.PasswordHasher`，配置节 `XiHan:Authentication:PasswordHasher`）。默认 **HMAC-SHA256、迭代 600,000 次（OWASP 建议）、32 字节随机盐、32 字节输出**；存储格式为自描述串 `version:iterations:algorithm:base64(salt):base64(hash)`，校验用定长比较（抗时序攻击），参数变更时 `NeedsRehash` 支持透明升级。密码**严禁明文落库**（`SysUserSecurity.Password`，`[JsonIgnore]`）。

- **验证码一次性消费**：邮箱登录码、2FA 邮箱/短信码、找回密码链接均**用后即焚**——读取即从缓存删除，防重放/爆破。邮箱码 6 位、10 分钟；找回密码令牌 30 分钟。
- **找回密码**（匿名，防用户枚举）：`POST /api/Auth/PasswordResetRequest` 无论邮箱是否存在都返回"已受理"；存在则签发一次性重置令牌（`auth:pwd-reset:{token}`，缓存 30 分钟，仅记录 token→userId，**不立即改密**）并邮件带链接。用户点链接后 `POST /api/Auth/ConsumePasswordResetToken` 设新密码（8-128 位），成功即失效令牌并落 `PasswordReset` 审计。
- **锁定与风控**：`SysUserSecurity` 记 `FailedLoginAttempts`/`LockoutEndTime` 等；`SysLoginLog` 记录每次尝试（含 `IsRiskLogin` 异地/新设备标记），按月分表，供暴力破解检测与合规审计。
- **注册**（`POST /api/Auth/Register`，匿名）：邮箱必填（全平台唯一登录标识），统一落默认注册租户、成为普通成员、**不分配角色**（权限由管理员后续授权）；欢迎邮件尽力而为、失败不阻断。

## 登录落点

"先登录后选租户"下，认证通过后由 `ResolveLoginLandingAsync` 决定进入哪里（进入后可随时 `POST /api/Auth/SwitchTenant` 切换）：

| 归属情况 | 落点 |
| --- | --- |
| 平台账号（`TenantId=0`）或超管角色 | **平台态**（控制中心：管理租户/用户/系统，或选租户进入） |
| 恰好一个可用租户成员 | **直接进入该租户**（免选择） |
| 零个或多个可用租户 | **平台态**（前端展示选择/原因） |

签发的令牌是否带 `TenantId` claim 随落点而定（平台态不带）。`GetUserInfo` 返回的 `UserInfoDto` 带 `IsPlatform`/`CanAccessPlatform` 供前端判断。切换到具体租户时会校验成员身份（超管可进任意租户）并在目标上下文重建授权快照。落点/切换与多租户强相关，成员关系、平台运维态、版本门控详见 [多租户](./multi-tenancy)。

## 个人中心

`ProfileAppService`（`[Authorize]`，按动态 API 约定推导出路由前缀 `api/Profile`）覆盖当前登录用户的自助管理，拆成多个 partial：

- **资料**：`GetProfile` / `UpdateProfile`（昵称、头像、性别、时区、语言等）；改用户名 `ChangeUserName`（内置账号禁改，改后通知）。
- **安全 · 密码**：`ChangePassword`（域服务校验旧密码，改后落 `PasswordChanged` 审计并通知）。
- **安全 · 2FA**：TOTP 用 `Setup2FA`（返回 `SharedKey` + `AuthenticatorUri` 供扫码）→ `Enable2FA`（提交验证码激活）；`Disable2FA` 关闭；email/phone 方式用 `Send2FASetupCode` 先下发再 `Enable2FA`。绑定/解绑落 `MfaBound`/`MfaUnbound` 审计。
- **联系方式换绑/验证**：换邮箱/手机走"发码→确认"两步（`ProfileVerificationPurpose` 区分用途：`ChangeEmail`/`ChangePhone`/`VerifyEmail`/`VerifyPhone`/`TwoFactorEmail`/`TwoFactorPhone`），需密码确认、校验唯一性。
- **会话/设备**：`GetSessions` 列出本人在线会话（标出当前会话）；`RevokeSession` 撤销指定设备；`RevokeOtherSessions` 一键踢掉其他所有端；`GetLoginLogs` 查本人登录历史。
- **第三方账号**：`GetLinkedAccounts` 列出已绑定的三方账号；`UnlinkAccount` 解绑（解绑前防止失去唯一登录方式）。
- **开发者 · API 凭证**（`SysUserApiCredential`）：`GetApiCredentials` 列出本人凭证；`CreateApiCredential`/`RotateApiCredentialSecret` 生成/滚动 `AppKey`（`ak_` 前缀，全局唯一）+ `AppSecret`（`sk_` 前缀，与账号密码同栈 `IPasswordHasher` 只存哈希，明文仅创建/滚动时返回一次）；`UpdateApiCredentialStatus`/`DeleteApiCredential` 启停/删除；每用户最多 5 个凭证，供服务端签名调用 OpenAPI，变更均触发安全通知。
- **偏好**：通知偏好 `GetNotificationPreference`/`UpdateNotificationPreference`（渠道 × 类型开关）；UI 偏好走 `UserSettingAppService` 按场景（Scene）+ key 存取，保存后经 SignalR 广播实现**多端同步**（服务端不解释 value，仅作跨端状态载体）。
- **账号注销**：`DeactivateAccount`（停用）/`DeleteAccount`（软删，`[HttpPost]` 显式锁定），均需密码确认并撤销全部会话。

## 相关页面

- [权限模型](./permission)：权限码、RBAC 继承、数据范围、字段脱敏、ABAC 约束、实时校验。
- [多租户](./multi-tenancy)：成员关系、平台运维态、租户切换、版本门控。
- [框架 · 认证模块](https://framework.docs.xihanfun.com/packages/authentication)：JWT / OAuth2 / TOTP / PBKDF2 的底层实现。
- [请求生命周期](./request-lifecycle)：认证/租户解析/会话闸门/授权在请求管道中的位置。
- [框架简介](./introduction)：后端驱动菜单的单一事实源 `PageRegistry`。
