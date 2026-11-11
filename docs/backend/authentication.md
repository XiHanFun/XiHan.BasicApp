# 统一认证

本页说明账号模型、令牌与会话、登录方式、锁屏与模仿登录，适用于后端开发者与接口对接方。

分工：框架只校验令牌（`Security.Jwt`，HS256）与判定策略；签发、刷新、会话、双因素、第三方登录、模仿与锁屏都是 Identity 包的操作与中间件。认证相关的配置都在 `BasicApp:Auth`，见[配置参考](../configuration)。

## 账号模型

账号是平台库实体 `SysUser`，不实现 `IMultiTenant`，以 `HomeTenantId` 记注册地租户（平台账号为 `platform`）。同一个登录名可以在不同租户各有一个账号；登录时填的 `tenant` 即注册地租户 ID，平台账号不填。账号还可以是别的租户的成员（`SysTenantMember`，含运维支持成员），登录后经切换租户进入。

| 状态 | 含义 |
| --- | --- |
| `Active` | 正常 |
| `Locked` | 管理员锁定（`users.lock`），由 `users.unlock` 解除；登录返回 `IDENTITY_DENIED_004` |
| `Disabled` | 管理员停用或本人停用，所有登录方式返回 `IDENTITY_DENIED_018` |

连续登录失败另有锁定期（`SysUserSecurity.LockoutEndAt`），不改账号状态，到期自动解除，期间登录返回 `IDENTITY_DENIED_004` 并带剩余秒数。

**系统内置账号**（`isSystemAccount`）：初始超级管理员与租户开通时的管理员。不能被停用、删除、锁定或由管理员重置密码，管理员也不能改它的邮箱与手机号；本人不能改用户名或关闭账号。一律返回 `IDENTITY_CONFLICT_014`。

**本人关闭账号**：`profile.deactivate-account`（停用，管理员可以重新启用）与 `profile.delete-account`（注销，账号软删除）。都要当前密码，模仿会话不能做，租户所有者要先转移所有权（`IDENTITY_CONFLICT_011`），成功后吊销本人全部会话。

**密码**：PBKDF2-SHA256 散列；至少 `PasswordMinLength`（8）位、同时含字母与数字；不能与最近 `PasswordHistoryDepth`（3）个密码相同。

## 令牌与会话

| 项 | 说明 |
| --- | --- |
| 访问令牌 | HS256 JWT，缺省 120 分钟（`AccessTokenMinutes`）；声明 `sub` 用户 ID、`tenant` 当前租户（平台没有）、`sid` 会话 ID、`role` 本会话激活的角色、模仿时 `act` |
| 刷新令牌 | 32 字节随机数，库里只存 SHA-256 散列，缺省 7 天（`RefreshTokenDays`）；每次刷新轮换、旧的立即失效，并顺延会话有效期；模仿会话的到期时间不顺延 |
| 会话 | `SysUserSession` 一行一个，ID 即 `sid`；记下浏览器、操作系统、设备类型、IP 与登录地点（IP 归属地） |
| 会话门禁 | `SessionGateMiddleware` 在 Authenticate 阶段、框架 JWT 认证之后核对会话仍有效（结果缓存 `SessionCacheSeconds` 秒，缺省 30），已吊销、已过期或用户不正常即 `IDENTITY_DENIED_001`；不带 `sid` 的令牌不经门禁 |

吊销会话的场景：注销、改密、管理员重置密码、找回密码、锁定或停用用户、删除用户、本人关闭账号、登录策略超限、强制下线、结束模仿、锁屏密码连错 5 次。吊销提交之后经 Realtime 推 `ForceLogout` 并断开这些会话的连接。

## 登录方式

开放哪些方式由系统参数 `identity.login-methods` 决定（逗号分隔的 `password`、`email`、`phone`、`passkey`，只认平台的）：没建即只有密码登录，密码登录总是开着；写了认不出的方式名即 `IDENTITY_VALIDATION_021`。`passkey` 还要宿主配了 `XiHan:Security:WebAuthn`，没配时列出也不算开放。`auth.login-config`（匿名 `GET`）把开放的方式、可用的第三方提供方、是否要图形验证码与是否开放注册返回给登录页。登录页取不到它时在表单上方常驻提示并可重试，取到之前「登录」不可点。

### 密码登录 `auth.login`

依次过三关：

1. 登录节流（`LoginThrottle`）：固定窗口只数失败，同一账号同一地址每个窗口 `LoginThrottleAccountLimit`（5）次、同一地址跨账号 `LoginThrottleIpLimit`（30）次，窗口 `LoginThrottleWindowSeconds`（60 秒）；超限 `IDENTITY_UNAVAILABLE_001` 并带 `retryAfter`。
2. 图形验证码（`CaptchaEnabled`，缺省开）：`auth.captcha` 发一张 4 位数字的 SVG，共享缓存里只存散列，`CaptchaSeconds`（300 秒）内用一次即作废，错了是 `IDENTITY_VALIDATION_007`。
3. 账号与密码：租户账号先核对租户存在、启用且没到期；连续失败达到 `MaxFailedAccessAttempts`（5）次锁定 `LockoutMinutes`（15）分钟。用户名错还是密码错不区分，都是 `IDENTITY_DENIED_003`。

账号开了双因素即返回挑战（见[双因素](#双因素)）；否则先过登录守卫（Access 实现：访问策略的登录时机、登录失败自动封禁），再开会话、签令牌对。

### 强制修改由他人设置的密码

密码由管理员创建或重置、由平台开通、由种子写入的账号都记着「密码由他人设置」，本人改密或找回密码后清除。系统参数 `identity.password`（JSON，只认平台的，没建即关）的 `forceChange` 决定要不要逼：

| 状态 | 效果 |
| --- | --- |
| 关（缺省） | 只留着标记，直接放行 |
| 开 | 这些账号**凭密码登录**（含两步验证的第二步）时会话一开即锁定（令牌响应 `mustChangePassword` 为 `true`，锁定原因 `PasswordChangeRequired`），并发一条安全站内信提醒；打开之前已有的标记同样生效，已经在线的会话下次登录才受影响 |

锁定中除 `profile.change-password` 与 `auth.logout` 外都是 `IDENTITY_DENIED_002`（`auth.refresh` 照常可用，新令牌还锁着）；前端弹改密表单，改完当场解开。验证码登录、第三方登录、通行密钥登录与模仿打开的会话不走这一步。参数写错即 `IDENTITY_VALIDATION_025`，只影响有标记的账号的密码登录。

### 验证码登录 `auth.send-login-code` → `auth.login-by-code`

方式要在 `identity.login-methods` 里开放。按邮箱（不分大小写）或已验证的手机号在登录租户里找账号；账号不存在、不唯一或停用时同样回答「已发送」，签发并计入限频，只是不发，不给探测账号的机会。

| 限制 | 配置键（缺省） |
| --- | --- |
| 码的有效期 | `VerificationCodeMinutes`（10 分钟） |
| 同一用途、同一目标两次发码的间隔 | `VerificationCodeIntervalSeconds`（60 秒） |
| 每个目标每天（UTC） | `VerificationCodeDailyPerTarget`（10 次） |
| 每个客户端地址每天（UTC） | `VerificationCodeDailyPerAddress`（50 次） |
| 输错即作废 | `VerificationCodeMaxFailures`（5 次），之后锁定 `VerificationCodeLockoutMinutes`（10 分钟） |

这组限制对找回密码、个人中心验证联系方式与双因素的邮件 / 短信码同样适用。码对、发码之后联系方式没变才认账号，再核对状态与锁定期；账号开了双因素照样要第二步。

### 第三方登录

GitHub、Gitee、Google、QQ、微信、企业微信、飞书、钉钉，配置在 `BasicApp:Auth:OAuth:Providers`，不分租户、不存库，启用且填了 `ClientId` 的才出现在登录页（见[配置参考](../configuration)）。

```text
浏览器 → GET /api/oauth/{provider}/challenge  → 提供方授权页
       ← GET /api/oauth/{provider}/callback   回调不发令牌，只发一次性票据（5 分钟），重定向到前端回调页
前端回调页 → POST /api/auth:external-login（票据 + 注册地）→ 令牌
```

- 两个端点是普通端点、不经内核管道；发起时生成随机 `state` 存进共享缓存，同时种一枚 HttpOnly Cookie，回调时两者都对上才认（Google 另用 PKCE）。
- 只认已绑定的账号，不自动建号：没绑定即 `IDENTITY_DENIED_015`。先用账号密码登录，到个人中心「第三方绑定」由本人领绑定意图（`profile.begin-external-bind`，10 分钟、用一次），回调之后再由本人确认（`profile.link-external`）；`profile.external-logins` 列出、`profile.unlink-external` 解绑。
- 第三方登录同样过租户、账号状态、锁定期、登录守卫与双因素。

### 通行密钥 `auth.begin-passkey-login` → `auth.login-by-passkey`

用指纹、面容或设备密码代替密码登录。选项生成与响应核对用框架的 `Security.WebAuthn`：宿主在 `XiHan:Security:WebAuthn` 配了依赖方 ID（前端页面所在的域名，本地开发用 `localhost`）与来源（前端页面的地址）才装，再在 `identity.login-methods` 列出 `passkey` 才开放；没配即 `IDENTITY_UNAVAILABLE_007`，没开放即 `IDENTITY_DENIED_016`。通行密钥绑定在依赖方 ID 上，换了域名已登记的全部失效。

```text
登录页 → POST /api/auth:begin-passkey-login           请求选项 + 一次性票据（不列允许的凭据，由认证器列出本站的通行密钥）
浏览器 navigator.credentials.get                     本人在设备上验证
登录页 → POST /api/auth:login-by-passkey（票据 + 断言）→ 令牌
```

- 不用填账号与注册地：按凭据 ID 找到登记的通行密钥，登录进它的注册地；没登记或已删除即 `IDENTITY_DENIED_022`。票据随选项的超时（缺省 300 秒）过期、只能用一次（`IDENTITY_VALIDATION_028`）。
- 框架核对来源、挑战、依赖方、用户在场与用户验证、签名和签名计数，失败是 `SECURITY_DENIED_005`（签名计数回退是 `SECURITY_DENIED_006`，可能是克隆的认证器），同时计入登录节流与登录守卫的认证失败。
- 同样过租户、账号状态、锁定期与登录守卫。认证器做了用户验证即顶替双因素；依赖方把 `UserVerification` 放宽到 `Preferred` 时，没做用户验证而账号开了双因素照样要第二步。
- 个人中心「安全设置」登记：`my-passkeys.begin-create` 先核对当前密码（错即 `IDENTITY_DENIED_003`），给创建选项与票据（列出已登记的凭据，同一认证器不重复登记）；浏览器创建之后 `my-passkeys.create` 连票据交回登记（票据只认发起的本人），成功后给本人发一条安全站内信。每人最多 10 个（`IDENTITY_CONFLICT_017`）。`my-passkeys.list` 列出、`my-passkeys.update` 改名、`my-passkeys.delete` 删除（删除后立即不能登录，设备上的那份要本人在设备上删）。登记、改名与删除模仿会话都不能做。
- 平台库只存公钥、签名计数、认证器型号与能否同步，不要求证明（`none`），私钥不离开认证器；用户句柄是用户 ID 的 16 个字节，不含个人信息。

### 双因素

方式有认证器（`Totp`）、邮箱（`Email`）与短信（`Phone`）。启用双因素的账号在第一步只得到挑战：

```json
{ "token": null, "twoFactor": { "ticket": "…", "methods": ["Totp", "Email"], "method": null, "codeSent": false, "expiresAt": "…" } }
```

- `method` 只在唯一可用的方式是认证器时预先选定，否则为 `null`，由用户选。
- `auth.send-two-factor-code` 凭票据把码发到账号已验证的邮箱或手机号（认证器不发码），`auth.verify-two-factor` 凭票据与码换令牌；第二步重新核对账号状态、锁定期与租户。
- 票据有效期 `TwoFactorTicketMinutes`（10 分钟，从签发起算、不顺延），输错 `TwoFactorTicketMaxFailures`（5）次即作废、须重新登录（`IDENTITY_DENIED_008`）；码错同时计入账号的失败计数与登录节流。认证器的码按时间步防重放。
- 验证码登录的第二步不能用同一种渠道（`IDENTITY_DENIED_017`）。

个人中心：`profile.setup-totp`（新密钥先加密暂存共享缓存 10 分钟）→ `profile.enable-two-factor`（用认证器的码确认才落库；邮箱与短信用 `profile.send-two-factor-code` 发的码确认）；`profile.disable-two-factor`（认证器要当前码，邮箱与短信要发到本人的码）。这几项模仿会话都不能做。不提供恢复码：丢了认证器由管理员 `users.reset-two-factor` 清掉（不能重置自己）。

## 刷新、注销、切换

| 操作 | 说明 |
| --- | --- |
| `auth.refresh` | 匿名，凭刷新令牌换新令牌对，刷新令牌轮换；会话已吊销或过期即 `IDENTITY_DENIED_001`，成员租户里的成员关系被移除或到期即 `IDENTITY_DENIED_006` |
| `auth.logout` | 吊销当前会话；模仿会话的发起人注销时一并吊销模仿会话 |
| `auth.switch-tenant` | 换到注册地租户、生效中的成员租户（含运维支持），平台账号还可回到平台：会话续用（同一 `sid`）、刷新令牌轮换、按新租户重算角色；先过目标一侧的登录守卫；模仿态不能切换 |
| `auth.activate-roles` / `auth.session-roles` | 换 / 查本会话激活的角色（动态职责分离，见[约束规则与审批](./approval)）；`auth.session-roles` 在 Access 包 |
| `auth.me` | 当前用户、租户（含显示名）、会话、角色与模仿发起人 |
| `auth.permissions` | Access 包：有效权限码、角色、按权限裁剪的菜单树与页面码 |

## 自助注册与找回密码

| 操作 | 说明 |
| --- | --- |
| `auth.register` | 匿名，注册平台账号：要在配置里打开（`SelfRegistrationEnabled`，缺省关，否则 `IDENTITY_DENIED_020`）；开了图形验证码时同样先过验证码；登录名在平台一侧唯一，邮箱不能与平台一侧的正常账号重复（`IDENTITY_CONFLICT_013`）；注册后立即可用、不带任何角色，配好邮件且有 `auth-welcome` 模板时发一封欢迎邮件 |
| `auth.request-password-reset` | 匿名，租户须存在、启用且没到期；按邮箱或已验证的手机号发验证码（验证类模板，标题「重置密码」）；账号不存在、不唯一也回答已发送，签发并计入限频，只是不发 |
| `auth.reset-password` | 匿名，凭验证码设新密码：密码先按策略校验（不合格不消费验证码），码对、联系方式没变、账号正常才认；设好后清掉登录失败锁定，并吊销这个账号的全部会话 |

`auth.login-config` 带 `registrationEnabled`，登录页据此决定出不出注册入口；注册页要先勾选同意隐私政策与服务条款才提交，两份文书按界面语言放在前端 `packages/views/_core/authentication/legal/<语言>/`，部署方改主体、联系方式或条款时直接改对应语言的 Markdown；找回密码入口总在。要进租户的人先注册平台账号，再由租户加为成员。找回密码发验证码、不发链接，不依赖前端地址配置；平台模板里的 `auth-password-reset`（链接式）目前没有用到。

## 锁屏

`auth.lock` 给当前会话记下锁屏密码的散列，之后这个会话除 `auth.unlock` 与 `auth.logout` 外都以 `IDENTITY_DENIED_002` 拒绝（`details.reason` 是 `ScreenLock`），只影响这一个端。解锁密码错一次是 `IDENTITY_DENIED_013`（带剩余次数），连错 5 次吊销会话（`IDENTITY_DENIED_014`）。强制改密的锁（`PasswordChangeRequired`）用不了锁屏密码（`IDENTITY_CONFLICT_006`），见[强制修改由他人设置的密码](#强制修改由他人设置的密码)；锁屏的会话也不能靠改密解开。

## 模仿登录

| 操作 | 说明 |
| --- | --- |
| `impersonation.candidates` | 可模仿的用户，分页（`pageIndex` 从 1 起、`pageSize` 1–100），关键字匹配登录名与显示名 |
| `impersonation.start` | 以目标身份另开会话并签发令牌（`sub` 是目标、`act` 是发起人），不写目标的登录痕迹；发起人会话保留 |
| `impersonation.stop` | 自助操作：吊销模仿会话，在发起人原会话上轮换刷新令牌、签回本人的令牌；原会话已失效即要重新登录 |

范围不填即发起人当前所在的上下文；租户主体只能在自己的租户里模仿；平台主体可指定租户（目标须是该租户的注册地用户或成员）或平台。不能模仿自己，不能在模仿态里再模仿；一条原会话同时只挂一条模仿会话，还没结束时再发起是 `IDENTITY_CONFLICT_015`（先 `impersonation.stop`，或等它到期）。权限码 `identity.impersonation.start`，开始与结束都进登录日志。

**权限上限**：不能模仿超级管理员；除平台侧持 `*` 的超级管理员外，发起人不能模仿持 `*` 的账号，目标在目标上下文持有的权限码都要在发起人手里。

**模仿态拒绝的权限码**：接管账号（重置密码、重置双因素、锁定、登录策略）、变更授权（角色的修改、授予、指派、继承，用户直授，用户数据范围，委托的新建、撤销与删除）、销毁数据（删除用户、角色、租户，初始化独立库）、OAuth 应用的修改（含重置客户端密钥）。持 `*` 也拦，`auth.permissions` 也不返回这些。

系统参数 `identity.impersonation`（JSON，没建即缺省，按发起人所在的一侧取，写错或越界即 `IDENTITY_VALIDATION_027`、不发起）：

| 字段 | 缺省 | 说明 |
| --- | --- | --- |
| `sessionMinutes` | `30` | 模仿会话存活分钟数，1 到 480。从发起算起的硬期限：`auth.refresh` 只换令牌不顺延，访问令牌也不超过它；到期后 `IDENTITY_DENIED_001`，要接着用就重新发起 |
| `notifyTarget` | `true` | 发起时给被模仿者发一条安全站内信（落在模仿会话所在的一侧，按被模仿者的界面语言，不看通知偏好）；投递失败只记日志，不挡模仿 |

## 登录策略

每个账号可以单独限制同时登录：`users.login-policy` 查看（`identity.users.read`），`users.set-login-policy` 设置（`identity.users.login-policy`），下次登录生效，已有会话不动。

| 设置 | 登录时的效果 |
| --- | --- |
| 不允许多端登录（`allowMultiLogin` 为 `false`） | 只保留新会话，其余吊销 |
| 允许多端、`maxLoginDevices` 为 1 到 99 | 保留新会话和最近的几个，超过的吊销最早的 |
| 允许多端、`maxLoginDevices` 为 0（缺省） | 不限 |

模仿会话不算设备。被吊销的会话收到 `ForceLogout`。

## 用户详情

`users.detail`（`GET /api/users/{id}:detail`，`identity.users.read`，可带 `timeZone` 按时区分天，缺省 UTC）给管理员看一个人的账号与活动，可见范围同 `users.get`：

| 字段 | 内容 |
| --- | --- |
| `user` | 资料（字段安全同样套用） |
| `security` | 邮箱与手机验证状态、双因素、是否锁定（管理员锁定或登录失败锁定期内）、锁定截止时间、连续失败次数；[外部成员](./multi-tenancy#外部成员)为空，账号由其注册地维护 |
| `today` | 今日登录次数、访问次数、在线时长，只算当前一侧；平台看租户的用户时为空 |
| `currentSession` | 这一侧最近活动的会话（IP 遮掩，模仿会话不算）；没有有效会话时为空 |

## 在线用户与会话管理

| 操作 | 权限 | 说明 |
| --- | --- | --- |
| `sessions.list` | `identity.sessions.read` | 一行一个有效会话：只看当前一侧，租户一侧按数据范围收窄，IP 遮掩，标出是否持有实时连接 |
| `sessions.summary` | `identity.sessions.read` | 有效会话数、有会话的用户数（同样按数据范围收窄）与持有实时连接的用户数 |
| `sessions.revoke` | `identity.sessions.revoke` | 批量吊销，可带给被下线用户看的原因；看不到的会话逐项 `IDENTITY_NOTFOUND_002`，调用方自己当前的会话逐项 `IDENTITY_CONFLICT_005` |
| `profile.sessions` / `profile.revoke-session` / `profile.revoke-other-sessions` | 登录即可 | 个人中心的「登录设备」：只按本人，不分侧、不套数据范围 |

## 下一步

- [权限管理](./permission) · [访问策略与权限条件](./access-policy) · [实时推送](./realtime)
