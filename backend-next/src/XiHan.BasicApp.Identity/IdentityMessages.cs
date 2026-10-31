// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包的消息键。
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。文案不进代码：错误只带键与参数，由宿主在 Egress 按调用方区域翻译。
/// 操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（<c>-</c> 换成 <c>_</c>），接口文档摘要与 MCP tool 描述共用。
/// </remarks>
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
    /// 操作 <c>users.disable</c> 的说明。
    /// </summary>
    public const string DisableUsersDescription = "users.disable.description";

    /// <summary>
    /// 操作 <c>users.enable</c> 的说明。
    /// </summary>
    public const string EnableUsersDescription = "users.enable.description";

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
    /// 会话不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string SessionNotFound = "identity.session.not_found";

    /// <summary>
    /// 不能强制下线自己当前的会话。
    /// </summary>
    public const string CurrentSessionNotRevocable = "identity.session.current";

    /// <summary>
    /// 用户不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string UserNotFound = "identity.user.not_found";

    /// <summary>
    /// 用户名已被占用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>userName</c>、<c>tenant</c>。
    /// </remarks>
    public const string UserNameTaken = "identity.user.name_taken";

    /// <summary>
    /// 请求的注册地租户与当前租户不一致。
    /// </summary>
    /// <remarks>
    /// 参数 <c>requested</c>、<c>current</c>。
    /// </remarks>
    public const string TenantMismatch = "identity.user.tenant_mismatch";

    /// <summary>
    /// 不能对自己的账号执行该操作。
    /// </summary>
    /// <remarks>
    /// 批量结果的逐项错误码 <c>IDENTITY_CONFLICT_002</c> 的文案。
    /// </remarks>
    public const string SelfOperation = "identity.user.self_operation";

    /// <summary>
    /// 用户是租户的所有者，不能删除或锁定。
    /// </summary>
    public const string TenantOwner = "identity.user.tenant_owner";

    /// <summary>
    /// 显示名只有空白。
    /// </summary>
    /// <remarks>
    /// <c>CreateUserRequest</c> 自校验的文案。
    /// </remarks>
    public const string DisplayNameBlank = "identity.user.display_name_blank";

    /// <summary>
    /// 邮箱格式不对。
    /// </summary>
    /// <remarks>
    /// 自校验的文案。
    /// </remarks>
    public const string EmailInvalid = "identity.user.email_invalid";

    /// <summary>
    /// 列表筛选不受支持。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>、<c>operator</c>。
    /// </remarks>
    public const string FilterUnsupported = "identity.user.filter_unsupported";

    /// <summary>
    /// 列表排序字段不受支持。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string SortUnsupported = "identity.user.sort_unsupported";

    /// <summary>
    /// 用户名或密码不正确。
    /// </summary>
    public const string InvalidCredentials = "identity.auth.invalid_credentials";

    /// <summary>
    /// 账号不可用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>status</c>。
    /// </remarks>
    public const string AccountUnavailable = "identity.auth.account_unavailable";

    /// <summary>
    /// 账号已停用。
    /// </summary>
    public const string AccountDisabled = "identity.auth.account_disabled";

    /// <summary>
    /// 登录失败次数过多，暂时锁定。
    /// </summary>
    /// <remarks>
    /// 参数 <c>seconds</c>。
    /// </remarks>
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
    /// 租户不存在或已停用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>tenantId</c>。
    /// </remarks>
    public const string TenantUnavailable = "identity.auth.tenant_unavailable";

    /// <summary>
    /// 密码太短。
    /// </summary>
    /// <remarks>
    /// 参数 <c>min</c>。
    /// </remarks>
    public const string PasswordTooShort = "identity.password.too_short";

    /// <summary>
    /// 密码须同时含字母与数字。
    /// </summary>
    public const string PasswordTooSimple = "identity.password.too_simple";

    /// <summary>
    /// 密码与最近的历史密码相同。
    /// </summary>
    /// <remarks>
    /// 参数 <c>depth</c>。
    /// </remarks>
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
    /// 不能进入该租户。
    /// </summary>
    /// <remarks>
    /// 参数 <c>tenantId</c>。
    /// </remarks>
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
    /// 模仿目标不可用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
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
    /// 登录被节流。
    /// </summary>
    /// <remarks>
    /// 参数 <c>retryAfter</c>。
    /// </remarks>
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
    /// 双因素方式不可用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>method</c>。
    /// </remarks>
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
    /// 不能按读受保护的字段筛选或排序。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string FieldProtected = "identity.field_security.protected";

    /// <summary>
    /// 只读字段不能填写或改动。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
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
    /// 会话已锁定，只放行解锁与注销。
    /// </summary>
    /// <remarks>
    /// 参数 <c>reason</c>（锁定原因，锁屏为 <c>ScreenLock</c>）。
    /// </remarks>
    public const string SessionLocked = "identity.auth.session_locked";

    /// <summary>
    /// 锁屏密码不能为空（没有密码的锁屏谁都能解开）。
    /// </summary>
    public const string LockPasswordRequired = "identity.auth.lock_password_required";

    /// <summary>
    /// 锁屏密码不对。
    /// </summary>
    /// <remarks>
    /// 参数 <c>remaining</c>（还能试几次）。
    /// </remarks>
    public const string UnlockPasswordWrong = "identity.auth.unlock_password_wrong";

    /// <summary>
    /// 解锁连错 5 次，会话已吊销。
    /// </summary>
    public const string UnlockExhausted = "identity.auth.unlock_exhausted";

    /// <summary>
    /// 会话的锁定原因不是锁屏，不能用锁屏密码解开。
    /// </summary>
    /// <remarks>
    /// 参数 <c>reason</c>。
    /// </remarks>
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
    /// 写本人调用安全设置的操作说明。
    /// </summary>
    /// <remarks>
    /// 操作 <c>my-api-credentials.update-settings</c> 的说明。
    /// </remarks>
    public const string MyApiCredentialsUpdateSettingsDescription = "my_api_credentials.update_settings.description";

    /// <summary>
    /// 新建凭证没填名称时的名称。
    /// </summary>
    public const string ApiCredentialDefaultName = "identity.api_credentials.default_name";

    /// <summary>
    /// 开放接口凭证到了上限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>max</c>。
    /// </remarks>
    public const string ApiCredentialLimitReached = "identity.api_credentials.limit_reached";

    /// <summary>
    /// 开放接口凭证不存在或不是本人的。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string ApiCredentialNotFound = "identity.api_credentials.not_found";

    /// <summary>
    /// 签名算法不认识。
    /// </summary>
    /// <remarks>
    /// 参数 <c>value</c>。
    /// </remarks>
    public const string ApiSignatureAlgorithmInvalid = "identity.api_credentials.algorithm_invalid";

    /// <summary>
    /// IP 白名单有一条不是 IP 或 CIDR。
    /// </summary>
    /// <remarks>
    /// 参数 <c>value</c>。
    /// </remarks>
    public const string ApiIpAllowListInvalid = "identity.api_credentials.ip_invalid";

    /// <summary>
    /// IP 白名单条数超过上限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>max</c>。
    /// </remarks>
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
    /// 这个第三方账号在所选注册地没有绑定账号。
    /// </summary>
    /// <remarks>
    /// 参数 <c>provider</c>。
    /// </remarks>
    public const string ExternalLoginNotBound = "identity.external.not_bound";

    /// <summary>
    /// 这个第三方账号已被同一注册地的别人绑定。
    /// </summary>
    /// <remarks>
    /// 参数 <c>provider</c>。
    /// </remarks>
    public const string ExternalLoginTaken = "identity.external.taken";

    /// <summary>
    /// 本人没有绑定这个提供商。
    /// </summary>
    /// <remarks>
    /// 参数 <c>provider</c>。
    /// </remarks>
    public const string ExternalLoginNotFound = "identity.external.not_found";

    /// <summary>
    /// 操作 <c>profile.begin-external-bind</c> 的说明。
    /// </summary>
    public const string BeginExternalBindDescription = "profile.begin_external_bind.description";

    /// <summary>
    /// 这个第三方登录方式不存在或没有启用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>provider</c>。
    /// </remarks>
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

    /// <summary>
    /// 操作 <c>profile.notification-preference</c> 的说明。
    /// </summary>
    public const string NotificationPreferenceDescription = "profile.notification_preference.description";

    /// <summary>
    /// 保存本人通知偏好的操作说明。
    /// </summary>
    /// <remarks>
    /// 操作 <c>profile.update-notification-preference</c> 的说明。
    /// </remarks>
    public const string UpdateNotificationPreferenceDescription = "profile.update_notification_preference.description";

    /// <summary>
    /// 操作 <c>profile.update</c> 的说明。
    /// </summary>
    public const string UpdateProfileDescription = "profile.update.description";

    /// <summary>
    /// 操作 <c>profile.change-username</c> 的说明。
    /// </summary>
    public const string ChangeUserNameDescription = "profile.change_username.description";

    /// <summary>
    /// 操作 <c>profile.remove-avatar</c> 的说明。
    /// </summary>
    public const string RemoveAvatarDescription = "profile.remove_avatar.description";

    /// <summary>
    /// 头像不是 JPEG / PNG 图片（按文件头认）或超过上限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>max</c>（MB）。
    /// </remarks>
    public const string AvatarInvalid = "identity.avatar.invalid";

    /// <summary>
    /// 新用户名与现在的相同。
    /// </summary>
    public const string UserNameUnchanged = "identity.user_name.unchanged";

    /// <summary>
    /// 距上次改用户名不到 90 天。
    /// </summary>
    /// <remarks>
    /// 参数 <c>days</c>（还要等几天）。
    /// </remarks>
    public const string UserNameChangeTooSoon = "identity.user_name.too_soon";

    /// <summary>
    /// 操作 <c>profile.send-verification-code</c> 的说明。
    /// </summary>
    public const string SendVerificationCodeDescription = "profile.send_verification_code.description";

    /// <summary>
    /// 操作 <c>profile.verify-contact</c> 的说明。
    /// </summary>
    public const string VerifyContactDescription = "profile.verify_contact.description";

    /// <summary>
    /// 操作 <c>profile.send-change-contact-code</c> 的说明。
    /// </summary>
    public const string SendChangeContactCodeDescription = "profile.send_change_contact_code.description";

    /// <summary>
    /// 操作 <c>profile.change-contact</c> 的说明。
    /// </summary>
    public const string ChangeContactDescription = "profile.change_contact.description";

    /// <summary>
    /// 验证码发得太频繁。
    /// </summary>
    /// <remarks>
    /// 参数 <c>seconds</c>。
    /// </remarks>
    public const string CodeTooFrequent = "identity.code.too_frequent";

    /// <summary>
    /// 这个邮箱或手机号今天收码的次数已达上限。
    /// </summary>
    public const string CodeTargetDailyLimit = "identity.code.target_daily_limit";

    /// <summary>
    /// 当前网络今天发码的次数已达上限。
    /// </summary>
    public const string CodeAddressDailyLimit = "identity.code.address_daily_limit";

    /// <summary>
    /// 验证码输错次数过多，暂时锁定。
    /// </summary>
    /// <remarks>
    /// 参数 <c>minutes</c>。
    /// </remarks>
    public const string CodeLocked = "identity.code.locked";

    /// <summary>
    /// 没有可用的邮件或短信发送配置。
    /// </summary>
    public const string CodeChannelUnavailable = "identity.code.channel_unavailable";

    /// <summary>
    /// 验证码无效或已过期。
    /// </summary>
    public const string CodeInvalid = "identity.code.invalid";

    /// <summary>
    /// 当前账号没有绑定要验证的联系方式。
    /// </summary>
    public const string ContactMissing = "identity.contact.missing";

    /// <summary>
    /// 新的联系方式格式不对，或与现在的相同。
    /// </summary>
    public const string ContactInvalid = "identity.contact.invalid";

    /// <summary>
    /// 联系方式已被同一租户的别的账号使用。
    /// </summary>
    public const string ContactTaken = "identity.contact.taken";

    /// <summary>
    /// 验证码邮件的用途标题：验证邮箱。
    /// </summary>
    public const string CodeTitleVerifyEmail = "identity.code.title.verify_email";

    /// <summary>
    /// 验证码短信的用途标题：验证手机号。
    /// </summary>
    public const string CodeTitleVerifyPhone = "identity.code.title.verify_phone";

    /// <summary>
    /// 验证码邮件的用途标题：更换邮箱。
    /// </summary>
    public const string CodeTitleChangeEmail = "identity.code.title.change_email";

    /// <summary>
    /// 验证码短信的用途标题：更换手机号。
    /// </summary>
    public const string CodeTitleChangePhone = "identity.code.title.change_phone";

    /// <summary>
    /// 操作 <c>auth.send-login-code</c> 的说明。
    /// </summary>
    public const string SendLoginCodeDescription = "auth.send_login_code.description";

    /// <summary>
    /// 操作 <c>auth.login-by-code</c> 的说明。
    /// </summary>
    public const string LoginByCodeDescription = "auth.login_by_code.description";

    /// <summary>
    /// 没有开放这种登录方式。
    /// </summary>
    /// <remarks>
    /// 参数 <c>method</c>。
    /// </remarks>
    public const string LoginMethodDisabled = "identity.login.method_disabled";

    /// <summary>
    /// 系统参数里的登录方式写得不对。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>。
    /// </remarks>
    public const string LoginMethodsInvalid = "identity.login.methods_invalid";

    /// <summary>
    /// 登录用的邮箱或手机号格式不对。
    /// </summary>
    public const string LoginTargetInvalid = "identity.login.target_invalid";

    /// <summary>
    /// 操作 <c>auth.send-two-factor-code</c> 的说明。
    /// </summary>
    public const string SendTwoFactorCodeDescription = "auth.send_two_factor_code.description";

    /// <summary>
    /// 操作 <c>profile.send-two-factor-code</c> 的说明。
    /// </summary>
    public const string SendTwoFactorSetupCodeDescription = "profile.send_two_factor_code.description";

    /// <summary>
    /// 要用作双因素的联系方式还没验证。
    /// </summary>
    public const string ContactUnverified = "identity.contact.unverified";

    /// <summary>
    /// 第二步只剩这次登录收码的那种方式。
    /// </summary>
    /// <remarks>
    /// 参数 <c>method</c>。
    /// </remarks>
    public const string TwoFactorSameAsLogin = "identity.two_factor.same_as_login";

    /// <summary>
    /// 验证码邮件的用途标题：邮箱两步验证。
    /// </summary>
    public const string CodeTitleTwoFactorEmail = "identity.code.title.two_factor_email";

    /// <summary>
    /// 验证码短信的用途标题：手机两步验证。
    /// </summary>
    public const string CodeTitleTwoFactorPhone = "identity.code.title.two_factor_phone";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(IdentityMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
