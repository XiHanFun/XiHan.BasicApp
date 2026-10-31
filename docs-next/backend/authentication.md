# 统一认证

本页说明 6.0 的账号模型、令牌与会话、登录方式、锁屏与模仿登录，适用于后端开发者与接口对接方。

认证的分工（方案 D10）：框架只校验令牌（`Security.Jwt`，HS256）与判定策略；签发、刷新、会话、双因素、第三方登录、模仿、锁屏都是 Identity 包的操作与中间件。

## 账号模型

账号是平台库实体 `SysUser`，不实现 `IMultiTenant`，以 `HomeTenantId` 记录注册地租户（平台账号为 `platform`）。同一个登录名可以在不同租户各有一个账号，登录时填写的 `tenant` 即注册地租户 ID，平台账号不填。账号还可以是其他租户的成员（`SysTenantMember`，含运维支持成员），登录后经切换租户进入。

| 状态 | 含义 |
| --- | --- |
| `Active` | 正常 |
| `Locked` | 连续登录失败或管理员锁定，到期自动解除 |
| `Disabled` | 管理员停用，所有登录方式返回 `IDENTITY_DENIED_018` |

## 令牌与会话

| 项 | 说明 |
| --- | --- |
| 访问令牌 | HS256 JWT，缺省 120 分钟（`BasicApp:Auth:AccessTokenMinutes`）；声明 `sub` 用户 ID、`tenant` 当前租户（平台没有）、`sid` 会话 ID、`role` 本会话激活的角色、模仿时 `act` |
| 刷新令牌 | 不透明字符串，库里只存散列，缺省 7 天；每次刷新轮换，旧的立即失效 |
| 会话 | `SysUserSession` 一行一个，ID 即 `sid`；记下浏览器、操作系统、设备类型与登录地点（IP 归属地） |
| 会话门禁 | `SessionGateMiddleware` 在 Authenticate 阶段核对会话仍有效（缓存 `SessionCacheSeconds` 秒），吊销 / 过期 / 用户非正常即 `IDENTITY_DENIED_001` |

吊销会话的场景：注销、改密、管理员重置密码、锁定或停用用户、删除用户、强制下线、锁屏密码连错 5 次。吊销提交后经 Realtime 推 `ForceLogout` 并断开该会话的连接。

## 登录方式

开放哪些方式由系统参数 `identity.login-methods` 决定（`password`、`email`、`phone`），`auth.login-config` 返回给登录页，连同可用的第三方提供方与是否要图形验证码。

### 密码登录 `auth.login`

依次过三关：

1. 登录节流（`LoginThrottle`）：固定窗口只数失败，同一账号同一地址缺省每分钟 5 次、同一地址跨账号 30 次，超限 `IDENTITY_UNAVAILABLE_001` 并带 `retryAfter`；
2. 图形验证码（`BasicApp:Auth:CaptchaEnabled`，缺省开）：4 位数字 SVG，共享缓存里只存散列，5 分钟内用一次即作废；
3. 账号与密码：PBKDF2；连续失败达到 `MaxFailedAccessAttempts`（5）锁定 `LockoutMinutes`（15）分钟。

之后过租户状态与登录守卫（访问策略的登录时机、登录失败自动封禁），再开会话。管理员重置过密码的账号返回 `mustChangePassword: true`。

### 验证码登录 `auth.send-login-code` → `auth.login-by-code`

按邮箱或已验证的手机号找账号；账号不存在、不唯一或停用时同样回答「已发送」，不给探测账号的机会。发送限频：同一目标间隔 60 秒、每天每目标 10 次、每地址 50 次；码 10 分钟有效、输错 5 次作废。

### 第三方登录

GitHub、Gitee、Google、QQ、微信、企业微信、飞书、钉钉，配置在 `BasicApp:Auth:OAuth:Providers`（见[配置参考](../configuration)）。

```text
浏览器 → GET /api/oauth/{provider}/challenge  → 提供方授权页
       ← GET /api/oauth/{provider}/callback   回调不发令牌，只发一次性票据，重定向到前端回调页
前端回调页 → POST /api/auth:external-login（票据 + 注册地）→ 令牌
```

- 只认已绑定的账号，不自动建号：没绑定即 `IDENTITY_DENIED_015`。先用账号密码登录，到个人中心「第三方绑定」里由本人发起绑定意图（`profile.begin-external-bind`）再确认（`profile.link-external`）。
- 第三方登录同样过租户、账号状态、锁定期与双因素。

### 双因素

启用双因素的账号在第一步只得到挑战：

```json
{ "token": null, "twoFactor": { "ticket": "…", "methods": ["Totp", "Email"], "method": "Totp", "codeSent": false, "expiresAt": "…" } }
```

- `auth.send-two-factor-code` 凭票据发邮件 / 短信码（要已验证的联系方式），`auth.verify-two-factor` 凭票据与码换令牌；
- 票据 10 分钟、输错 5 次即作废；认证器按时间步防重放；
- 验证码登录的第二步不能用同一种渠道。

个人中心：`profile.setup-totp`（新密钥先暂存 10 分钟，确认才加密落库）、`profile.enable-two-factor` / `profile.disable-two-factor`（关闭要当前码，模仿会话一律拒绝）；管理员 `users.reset-two-factor`（不能重置自己）。

## 刷新、注销、切换

| 操作 | 说明 |
| --- | --- |
| `auth.refresh` | 匿名操作，凭刷新令牌换新令牌对，刷新令牌轮换 |
| `auth.logout` | 吊销当前会话 |
| `auth.switch-tenant` | 会话续用（同一 `sid`）、刷新令牌轮换、按新租户重算角色；目标是注册地租户、生效中的成员租户或平台；先过目标一侧的登录守卫；模仿态不能切换 |
| `auth.activate-roles` / `auth.session-roles` | 换本会话激活的角色（动态职责分离，见[约束规则与审批](./approval)） |
| `auth.me` / `auth.permissions` | 当前用户、模仿横幅；权限码、角色、菜单树、页面码 |

## 锁屏

`auth.lock` 给当前会话记下锁屏密码的散列，之后这个会话除 `auth.unlock` 与 `auth.logout` 外都以 `IDENTITY_DENIED_002` 拒绝，只影响这一个端。解锁密码连错 5 次吊销会话（`IDENTITY_DENIED_014`）。5.x 的 HTTP 423 在 6.0 统一为这个错误码（403）。

## 模仿登录

| 操作 | 说明 |
| --- | --- |
| `impersonation.candidates` | 可模仿的用户，分页（`pageIndex` 从 1 起、`pageSize` 1–100） |
| `impersonation.start` | 以目标身份另开会话并签发令牌（`sub` 是目标、`act` 是发起人），不写目标的登录痕迹；发起人会话保留 |
| `impersonation.stop` | 结束模仿，回到发起人的会话 |

范围不填即发起人当前所在的上下文；租户主体只能在自己的租户里模仿；平台主体可指定租户或平台。不能模仿自己，不能在模仿态里再模仿。权限码 `identity.impersonation.start`，开始与结束都进登录日志。

## 在线用户与会话管理

| 操作 | 权限 | 说明 |
| --- | --- | --- |
| `sessions.list` | `identity.sessions.read` | 一行一个有效会话：只看当前一侧、按数据范围收窄、IP 遮掩，标出实时在线与调用方当前会话 |
| `sessions.summary` | `identity.sessions.read` | 汇总 |
| `sessions.revoke` | `identity.sessions.revoke` | 批量吊销，可带给被下线用户看的原因；不能下线自己当前的会话 |
| `profile.sessions` / `profile.revoke-session` / `profile.revoke-other-sessions` | 登录即可 | 个人中心的「登录设备」 |

## 尚未实现

- 自助注册与找回密码（方案 5.8 的 `auth.register`、`auth.request-password-reset`、`auth.reset-password`）当前没有对应操作，前端登录页不显示这两个入口；平台模板里已有 `auth-password-reset`、`auth-welcome` 邮件模板。
- 恢复码（5.x 也没有）。

## 下一步

- [权限管理](./permission) · [访问策略与权限条件](./access-policy) · [实时推送](./realtime)
