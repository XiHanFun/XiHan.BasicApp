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

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(IdentityMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
