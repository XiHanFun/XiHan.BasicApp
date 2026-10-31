# XiHan.BasicApp.Identity

Tier 1 身份能力包：账号、认证、会话与个人中心。提供能力 `basicapp.identity@1`；账号是平台库实体，以 `HomeTenantId` 记录注册地租户（平台账号为 `platform`）。

| 目录 | 内容 |
| --- | --- |
| `Operations/Auth` | 登录（密码、邮箱 / 手机验证码、第三方）、两段式双因素、图形验证码、登录配置、刷新、注销、锁屏、切换租户、激活角色、`auth.me`；自助注册与找回密码（`auth.register`、`auth.request-password-reset`、`auth.reset-password`） |
| `Operations/Users` | 用户管理：增删改查、启停、锁定与解锁、重置密码、重置双因素；列表、详情与批量操作按数据范围收窄 |
| `Operations/Profile` | 个人中心：资料与头像、改密、改用户名、联系方式验证与换绑、双因素设置、第三方账号绑定、登录设备、通知偏好、活跃度 |
| `Operations/Sessions` | 在线用户：会话列表、汇总、批量吊销 |
| `Operations/Impersonation` | 模仿登录：候选人、开始、结束 |
| `Operations/UserSettings` · `MyApiCredentials` | 用户设置的跨端同步；本人的开放接口凭据 |
| `Domain` | `SysUser`、`SysUserSecurity`、`SysUserSession`、`SysPasswordHistory` 等实体；`UserAccountService`（按名 / 联系方式找账号、设密码、吊销会话）、`PasswordPolicy`、`ContactCodes`、`TenantDirectoryGate`（租户存在、启用、没到期） |
| `Infrastructure` | 令牌签发、会话门禁、登录节流与守卫、验证码与票据、第三方登录、在线用户小组件，以及给其他包的 Contracts 实现（用户目录、通知偏好、会话历史） |
| `Middleware/SessionGateMiddleware` | Authenticate 阶段核对会话：吊销、锁屏、模仿会话的发起人 |

## 配置

`BasicApp:Auth`：令牌有效期、登录失败锁定、密码策略、会话门禁缓存、图形验证码、登录节流、双因素、邮件 / 短信验证码与自助注册开关（`SelfRegistrationEnabled`，缺省关）；第三方登录在 `BasicApp:Auth:OAuth`。键与缺省见 docs-next 的配置页。

## 约束

- 错误码 `IDENTITY_<KIND>_<NNN>`、消息键与 10 种区域文案在 `IdentityErrors` / `IdentityMessages` / `Localization/`，常量与文案由 `Architecture.Tests` 的 `PackageStructureTests` 核对。
- 匿名操作（登录、刷新、验证码、注册与找回密码）登记在 `OperationContractTests` 的匿名白名单；超出 `Http` 暴露的操作由 `OperationExposureTests` 逐个登记。
