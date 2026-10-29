// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包的消息键；各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。文案不进代码：错误只带键与参数，由宿主在 Egress 按调用方区域翻译。
/// 操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（<c>-</c> 换成 <c>_</c>），接口文档摘要与 MCP tool 描述共用。
/// </summary>
public static class IdentityMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.identity";

    /// <summary>
    /// 操作 <c>users.create</c> 的说明。
    /// </summary>
    public const string CreateUserDescription = "users.create.description";

    /// <summary>
    /// 操作 <c>users.get</c> 的说明。
    /// </summary>
    public const string GetUserDescription = "users.get.description";

    /// <summary>
    /// 操作 <c>users.list</c> 的说明。
    /// </summary>
    public const string ListUsersDescription = "users.list.description";

    /// <summary>
    /// 操作 <c>users.update</c> 的说明。
    /// </summary>
    public const string UpdateUserDescription = "users.update.description";

    /// <summary>
    /// 操作 <c>users.delete</c> 的说明。
    /// </summary>
    public const string DeleteUsersDescription = "users.delete.description";

    /// <summary>
    /// 操作 <c>users.lock</c> 的说明。
    /// </summary>
    public const string LockUsersDescription = "users.lock.description";

    /// <summary>
    /// 操作 <c>users.unlock</c> 的说明。
    /// </summary>
    public const string UnlockUsersDescription = "users.unlock.description";

    /// <summary>
    /// 操作 <c>users.reset-password</c> 的说明。
    /// </summary>
    public const string ResetPasswordDescription = "users.reset_password.description";

    /// <summary>
    /// 操作 <c>auth.login</c> 的说明。
    /// </summary>
    public const string LoginDescription = "auth.login.description";

    /// <summary>
    /// 操作 <c>auth.refresh</c> 的说明。
    /// </summary>
    public const string RefreshDescription = "auth.refresh.description";

    /// <summary>
    /// 操作 <c>auth.logout</c> 的说明。
    /// </summary>
    public const string LogoutDescription = "auth.logout.description";

    /// <summary>
    /// 操作 <c>auth.me</c> 的说明。
    /// </summary>
    public const string MeDescription = "auth.me.description";

    /// <summary>
    /// 操作 <c>profile.change-password</c> 的说明。
    /// </summary>
    public const string ChangePasswordDescription = "profile.change_password.description";

    /// <summary>
    /// 操作 <c>user-settings.get</c> 的说明。
    /// </summary>
    public const string GetUserSettingDescription = "user_settings.get.description";

    /// <summary>
    /// 操作 <c>user-settings.update</c> 的说明。
    /// </summary>
    public const string UpdateUserSettingDescription = "user_settings.update.description";

    /// <summary>
    /// 操作 <c>sessions.list</c> 的说明。
    /// </summary>
    public const string ListSessionsDescription = "sessions.list.description";

    /// <summary>
    /// 操作 <c>sessions.summary</c> 的说明。
    /// </summary>
    public const string GetSessionSummaryDescription = "sessions.summary.description";

    /// <summary>
    /// 操作 <c>sessions.revoke</c> 的说明。
    /// </summary>
    public const string RevokeSessionsDescription = "sessions.revoke.description";

    /// <summary>
    /// 会话不存在；参数 <c>id</c>。
    /// </summary>
    public const string SessionNotFound = "identity.session.not_found";

    /// <summary>
    /// 不能强制下线自己当前的会话。
    /// </summary>
    public const string CurrentSessionNotRevocable = "identity.session.current";

    /// <summary>
    /// 用户不存在；参数 <c>id</c>。
    /// </summary>
    public const string UserNotFound = "identity.user.not_found";

    /// <summary>
    /// 用户名已被占用；参数 <c>userName</c>、<c>tenant</c>。
    /// </summary>
    public const string UserNameTaken = "identity.user.name_taken";

    /// <summary>
    /// 请求的注册地租户与当前租户不一致；参数 <c>requested</c>、<c>current</c>。
    /// </summary>
    public const string TenantMismatch = "identity.user.tenant_mismatch";

    /// <summary>
    /// 不能对自己的账号执行该操作（批量结果的逐项错误码 <c>IDENTITY_CONFLICT_002</c> 的文案）。
    /// </summary>
    public const string SelfOperation = "identity.user.self_operation";

    /// <summary>
    /// 显示名只有空白；<c>CreateUserRequest</c> 自校验的文案。
    /// </summary>
    public const string DisplayNameBlank = "identity.user.display_name_blank";

    /// <summary>
    /// 邮箱格式不对；自校验的文案。
    /// </summary>
    public const string EmailInvalid = "identity.user.email_invalid";

    /// <summary>
    /// 列表筛选不受支持；参数 <c>field</c>、<c>operator</c>。
    /// </summary>
    public const string FilterUnsupported = "identity.user.filter_unsupported";

    /// <summary>
    /// 列表排序字段不受支持；参数 <c>field</c>。
    /// </summary>
    public const string SortUnsupported = "identity.user.sort_unsupported";

    /// <summary>
    /// 用户名或密码不正确。
    /// </summary>
    public const string InvalidCredentials = "identity.auth.invalid_credentials";

    /// <summary>
    /// 账号不可用；参数 <c>status</c>。
    /// </summary>
    public const string AccountUnavailable = "identity.auth.account_unavailable";

    /// <summary>
    /// 登录失败次数过多，暂时锁定；参数 <c>seconds</c>。
    /// </summary>
    public const string LockedOut = "identity.auth.locked_out";

    /// <summary>
    /// 会话已失效。
    /// </summary>
    public const string SessionInvalid = "identity.auth.session_invalid";

    /// <summary>
    /// 当前令牌不带会话，不能注销。
    /// </summary>
    public const string NoSession = "identity.auth.no_session";

    /// <summary>
    /// 租户不存在或已停用；参数 <c>tenantId</c>。
    /// </summary>
    public const string TenantUnavailable = "identity.auth.tenant_unavailable";

    /// <summary>
    /// 密码太短；参数 <c>min</c>。
    /// </summary>
    public const string PasswordTooShort = "identity.password.too_short";

    /// <summary>
    /// 密码须同时含字母与数字。
    /// </summary>
    public const string PasswordTooSimple = "identity.password.too_simple";

    /// <summary>
    /// 密码与最近的历史密码相同；参数 <c>depth</c>。
    /// </summary>
    public const string PasswordReused = "identity.password.reused";

    /// <summary>
    /// 操作 <c>auth.switch-tenant</c> 的说明。
    /// </summary>
    public const string SwitchTenantDescription = "auth.switch_tenant.description";

    /// <summary>
    /// 操作 <c>auth.activate-roles</c> 的说明。
    /// </summary>
    public const string ActivateRolesDescription = "auth.activate_roles.description";

    /// <summary>
    /// 操作 <c>impersonation.start</c> 的说明。
    /// </summary>
    public const string StartImpersonationDescription = "impersonation.start.description";

    /// <summary>
    /// 操作 <c>impersonation.stop</c> 的说明。
    /// </summary>
    public const string StopImpersonationDescription = "impersonation.stop.description";

    /// <summary>
    /// 操作 <c>impersonation.candidates</c> 的说明。
    /// </summary>
    public const string ImpersonationCandidatesDescription = "impersonation.candidates.description";

    /// <summary>
    /// 不能进入该租户；参数 <c>tenantId</c>。
    /// </summary>
    public const string TenantNotAccessible = "identity.auth.tenant_not_accessible";

    /// <summary>
    /// 当前会话处于模仿态。
    /// </summary>
    public const string ImpersonationActive = "identity.impersonation.active";

    /// <summary>
    /// 当前不处于模仿态。
    /// </summary>
    public const string NotImpersonating = "identity.impersonation.not_active";

    /// <summary>
    /// 模仿目标不可用；参数 <c>id</c>。
    /// </summary>
    public const string ImpersonationTargetUnavailable = "identity.impersonation.target_unavailable";

    /// <summary>
    /// 操作 <c>auth.verify-two-factor</c> 的说明。
    /// </summary>
    public const string VerifyTwoFactorDescription = "auth.verify_two_factor.description";

    /// <summary>
    /// 操作 <c>auth.captcha</c> 的说明。
    /// </summary>
    public const string IssueCaptchaDescription = "auth.captcha.description";

    /// <summary>
    /// 操作 <c>auth.login-config</c> 的说明。
    /// </summary>
    public const string LoginConfigDescription = "auth.login_config.description";

    /// <summary>
    /// 操作 <c>profile.get</c> 的说明。
    /// </summary>
    public const string GetProfileDescription = "profile.get.description";

    /// <summary>
    /// 操作 <c>profile.setup-totp</c> 的说明。
    /// </summary>
    public const string SetupTotpDescription = "profile.setup_totp.description";

    /// <summary>
    /// 操作 <c>profile.enable-two-factor</c> 的说明。
    /// </summary>
    public const string EnableTwoFactorDescription = "profile.enable_two_factor.description";

    /// <summary>
    /// 操作 <c>profile.disable-two-factor</c> 的说明。
    /// </summary>
    public const string DisableTwoFactorDescription = "profile.disable_two_factor.description";

    /// <summary>
    /// 操作 <c>users.reset-two-factor</c> 的说明。
    /// </summary>
    public const string ResetTwoFactorDescription = "users.reset_two_factor.description";

    /// <summary>
    /// 图形验证码不对。
    /// </summary>
    public const string CaptchaInvalid = "identity.auth.captcha_invalid";

    /// <summary>
    /// 登录被节流；参数 <c>retryAfter</c>。
    /// </summary>
    public const string LoginThrottled = "identity.auth.login_throttled";

    /// <summary>
    /// 两步验证票据失效。
    /// </summary>
    public const string TwoFactorTicketInvalid = "identity.two_factor.ticket_invalid";

    /// <summary>
    /// 双因素验证码不对。
    /// </summary>
    public const string TwoFactorCodeInvalid = "identity.two_factor.code_invalid";

    /// <summary>
    /// 双因素方式不可用；参数 <c>method</c>。
    /// </summary>
    public const string TwoFactorMethodUnavailable = "identity.two_factor.method_unavailable";

    /// <summary>
    /// 开着双因素却没有可用方式。
    /// </summary>
    public const string TwoFactorUnavailable = "identity.two_factor.unavailable";

    /// <summary>
    /// 认证器已经绑定。
    /// </summary>
    public const string TotpAlreadyEnabled = "identity.two_factor.totp_already_enabled";

    /// <summary>
    /// 没有待确认的认证器密钥。
    /// </summary>
    public const string TotpSetupMissing = "identity.two_factor.totp_setup_missing";

    /// <summary>
    /// 不能按读受保护的字段筛选或排序；参数 <c>field</c>。
    /// </summary>
    public const string FieldProtected = "identity.field_security.protected";

    /// <summary>
    /// 只读字段不能填写或改动；参数 <c>field</c>。
    /// </summary>
    public const string FieldReadOnly = "identity.field_security.read_only";

    /// <summary>
    /// 操作 <c>auth.lock</c> 的说明。
    /// </summary>
    public const string LockDescription = "auth.lock.description";

    /// <summary>
    /// 操作 <c>auth.unlock</c> 的说明。
    /// </summary>
    public const string UnlockDescription = "auth.unlock.description";

    /// <summary>
    /// 会话已锁定，只放行解锁与注销；参数 <c>reason</c>（锁定原因，锁屏为 <c>ScreenLock</c>）。
    /// </summary>
    public const string SessionLocked = "identity.auth.session_locked";

    /// <summary>
    /// 锁屏密码不能为空（没有密码的锁屏谁都能解开）。
    /// </summary>
    public const string LockPasswordRequired = "identity.auth.lock_password_required";

    /// <summary>
    /// 锁屏密码不对；参数 <c>remaining</c>（还能试几次）。
    /// </summary>
    public const string UnlockPasswordWrong = "identity.auth.unlock_password_wrong";

    /// <summary>
    /// 解锁连错 5 次，会话已吊销。
    /// </summary>
    public const string UnlockExhausted = "identity.auth.unlock_exhausted";

    /// <summary>
    /// 会话的锁定原因不是锁屏，不能用锁屏密码解开；参数 <c>reason</c>。
    /// </summary>
    public const string LockReasonUnsupported = "identity.auth.lock_reason_unsupported";

    /// <summary>
    /// 操作 <c>my-api-credentials.list</c> 的说明。
    /// </summary>
    public const string MyApiCredentialsListDescription = "my_api_credentials.list.description";

    /// <summary>
    /// 操作 <c>my-api-credentials.create</c> 的说明。
    /// </summary>
    public const string MyApiCredentialsCreateDescription = "my_api_credentials.create.description";

    /// <summary>
    /// 操作 <c>my-api-credentials.update</c> 的说明。
    /// </summary>
    public const string MyApiCredentialsUpdateDescription = "my_api_credentials.update.description";

    /// <summary>
    /// 操作 <c>my-api-credentials.rotate-secret</c> 的说明。
    /// </summary>
    public const string MyApiCredentialsRotateSecretDescription = "my_api_credentials.rotate_secret.description";

    /// <summary>
    /// 操作 <c>my-api-credentials.delete</c> 的说明。
    /// </summary>
    public const string MyApiCredentialsDeleteDescription = "my_api_credentials.delete.description";

    /// <summary>
    /// 操作 <c>my-api-credentials.settings</c> 的说明。
    /// </summary>
    public const string MyApiCredentialsSettingsDescription = "my_api_credentials.settings.description";

    /// <summary>
    /// 操作 <c>my-api-credentials.update-settings</c> 的说明。
    /// </summary>
    public const string MyApiCredentialsUpdateSettingsDescription = "my_api_credentials.update_settings.description";

    /// <summary>
    /// 新建凭证没填名称时的名称。
    /// </summary>
    public const string ApiCredentialDefaultName = "identity.api_credentials.default_name";

    /// <summary>
    /// 开放接口凭证到了上限；参数 <c>max</c>。
    /// </summary>
    public const string ApiCredentialLimitReached = "identity.api_credentials.limit_reached";

    /// <summary>
    /// 开放接口凭证不存在或不是本人的；参数 <c>id</c>。
    /// </summary>
    public const string ApiCredentialNotFound = "identity.api_credentials.not_found";

    /// <summary>
    /// 签名算法不认识；参数 <c>value</c>。
    /// </summary>
    public const string ApiSignatureAlgorithmInvalid = "identity.api_credentials.algorithm_invalid";

    /// <summary>
    /// IP 白名单有一条不是 IP 或 CIDR；参数 <c>value</c>。
    /// </summary>
    public const string ApiIpAllowListInvalid = "identity.api_credentials.ip_invalid";

    /// <summary>
    /// IP 白名单条数超过上限；参数 <c>max</c>。
    /// </summary>
    public const string ApiIpAllowListTooLong = "identity.api_credentials.ip_too_many";

    /// <summary>
    /// 操作 <c>auth.external-login</c> 的说明。
    /// </summary>
    public const string ExternalLoginDescription = "auth.external_login.description";

    /// <summary>
    /// 操作 <c>profile.external-logins</c> 的说明。
    /// </summary>
    public const string ExternalLoginsDescription = "profile.external_logins.description";

    /// <summary>
    /// 操作 <c>profile.link-external</c> 的说明。
    /// </summary>
    public const string LinkExternalDescription = "profile.link_external.description";

    /// <summary>
    /// 操作 <c>profile.unlink-external</c> 的说明。
    /// </summary>
    public const string UnlinkExternalDescription = "profile.unlink_external.description";

    /// <summary>
    /// 第三方登录的票据不存在、过期、已用过或用途不符。
    /// </summary>
    public const string ExternalTicketInvalid = "identity.external.ticket_invalid";

    /// <summary>
    /// 这个第三方账号在所选注册地没有绑定账号；参数 <c>provider</c>。
    /// </summary>
    public const string ExternalLoginNotBound = "identity.external.not_bound";

    /// <summary>
    /// 这个第三方账号已被同一注册地的别人绑定；参数 <c>provider</c>。
    /// </summary>
    public const string ExternalLoginTaken = "identity.external.taken";

    /// <summary>
    /// 本人没有绑定这个提供商；参数 <c>provider</c>。
    /// </summary>
    public const string ExternalLoginNotFound = "identity.external.not_found";

    /// <summary>
    /// 操作 <c>profile.begin-external-bind</c> 的说明。
    /// </summary>
    public const string BeginExternalBindDescription = "profile.begin_external_bind.description";

    /// <summary>
    /// 这个第三方登录方式不存在或没有启用；参数 <c>provider</c>。
    /// </summary>
    public const string ExternalProviderNotFound = "identity.external.provider_not_found";

    /// <summary>
    /// 操作 <c>profile.sessions</c> 的说明。
    /// </summary>
    public const string MySessionsDescription = "profile.sessions.description";

    /// <summary>
    /// 操作 <c>profile.revoke-session</c> 的说明。
    /// </summary>
    public const string RevokeMySessionDescription = "profile.revoke_session.description";

    /// <summary>
    /// 操作 <c>profile.revoke-other-sessions</c> 的说明。
    /// </summary>
    public const string RevokeMyOtherSessionsDescription = "profile.revoke_other_sessions.description";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(IdentityMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
