// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包的错误码：<c>IDENTITY_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </summary>
public static class IdentityErrors
{
    /// <summary>
    /// 用户不存在（或不在当前租户）。
    /// </summary>
    public const string UserNotFound = "IDENTITY_NOTFOUND_001";

    /// <summary>
    /// 用户名在该租户内已被占用。
    /// </summary>
    public const string UserNameTaken = "IDENTITY_CONFLICT_001";

    /// <summary>
    /// 不能对自己的账号执行该操作（删除、锁定）。
    /// </summary>
    public const string SelfOperation = "IDENTITY_CONFLICT_002";

    /// <summary>
    /// 当前会话处于模仿态：不能再次发起模仿，也不能切换租户。
    /// </summary>
    public const string ImpersonationActive = "IDENTITY_CONFLICT_003";

    /// <summary>
    /// 请求指定的注册地租户与当前租户不一致。
    /// </summary>
    public const string TenantMismatch = "IDENTITY_VALIDATION_001";

    /// <summary>
    /// 列表的筛选或排序字段不受支持。
    /// </summary>
    public const string QueryUnsupported = "IDENTITY_VALIDATION_002";

    /// <summary>
    /// 新密码不符合密码策略。
    /// </summary>
    public const string PasswordPolicy = "IDENTITY_VALIDATION_003";

    /// <summary>
    /// 新密码与最近的历史密码相同。
    /// </summary>
    public const string PasswordReused = "IDENTITY_VALIDATION_004";

    /// <summary>
    /// 当前令牌不带会话，不能注销。
    /// </summary>
    public const string NoSession = "IDENTITY_VALIDATION_005";

    /// <summary>
    /// 当前不处于模仿态，没有可结束的模仿。
    /// </summary>
    public const string NotImpersonating = "IDENTITY_VALIDATION_006";

    /// <summary>
    /// 会话已吊销、已过期或不存在；客户端应重新登录。
    /// </summary>
    public const string SessionInvalid = "IDENTITY_DENIED_001";

    /// <summary>
    /// 用户名或密码不正确（不区分哪一个）。
    /// </summary>
    public const string InvalidCredentials = "IDENTITY_DENIED_003";

    /// <summary>
    /// 账号被禁用、锁定或处于登录失败锁定期。
    /// </summary>
    public const string AccountUnavailable = "IDENTITY_DENIED_004";

    /// <summary>
    /// 登录指定的租户不存在或已停用（不核对用户名）。
    /// </summary>
    public const string TenantUnavailable = "IDENTITY_DENIED_005";

    /// <summary>
    /// 当前用户不能进入该租户（不是注册地也不是成员），或租户主体试图离开自己的租户。
    /// </summary>
    public const string TenantNotAccessible = "IDENTITY_DENIED_006";

    /// <summary>
    /// 模仿目标不可用：不存在、非正常状态、是自己，或不在目标上下文里。
    /// </summary>
    public const string ImpersonationTargetUnavailable = "IDENTITY_DENIED_007";

    /// <summary>
    /// 图形验证码缺失、错误、过期或已用过。
    /// </summary>
    public const string CaptchaInvalid = "IDENTITY_VALIDATION_007";

    /// <summary>
    /// 双因素方式不可用（目前只有认证器能开启与验证）。
    /// </summary>
    public const string TwoFactorMethodUnavailable = "IDENTITY_VALIDATION_008";

    /// <summary>
    /// 没有待确认的认证器密钥（没生成或已过期）。
    /// </summary>
    public const string TotpSetupMissing = "IDENTITY_VALIDATION_009";

    /// <summary>
    /// 个人中心开关双因素时验证码不对。
    /// </summary>
    public const string TwoFactorCodeRejected = "IDENTITY_VALIDATION_010";

    /// <summary>
    /// 认证器已经绑定（换设备须先关闭）。
    /// </summary>
    public const string TotpAlreadyEnabled = "IDENTITY_CONFLICT_004";

    /// <summary>
    /// 两步验证票据不存在、已过期或已作废，须重新登录。
    /// </summary>
    public const string TwoFactorTicketInvalid = "IDENTITY_DENIED_008";

    /// <summary>
    /// 登录第二步的验证码不对。
    /// </summary>
    public const string TwoFactorCodeInvalid = "IDENTITY_DENIED_009";

    /// <summary>
    /// 账号开着双因素却没有可用的方式（如认证器密钥丢失），须管理员重置。
    /// </summary>
    public const string TwoFactorUnavailable = "IDENTITY_DENIED_010";

    /// <summary>
    /// 按字段安全读受保护的字段筛选或排序；参数 <c>field</c>。
    /// </summary>
    public const string FieldProtected = "IDENTITY_DENIED_011";

    /// <summary>
    /// 填写或改动了字段安全里只读的字段；参数 <c>field</c>。
    /// </summary>
    public const string FieldReadOnly = "IDENTITY_DENIED_012";

    /// <summary>
    /// 登录失败太频繁，本窗口内暂停尝试；参数 <c>retryAfter</c> 秒。
    /// </summary>
    public const string LoginThrottled = "IDENTITY_UNAVAILABLE_001";

    /// <summary>
    /// 会话不存在、已失效或不在当前一侧与数据范围内；参数 <c>id</c>。
    /// </summary>
    public const string SessionNotFound = "IDENTITY_NOTFOUND_002";

    /// <summary>
    /// 不能强制下线自己当前的会话（用退出登录）。
    /// </summary>
    public const string CurrentSessionNotRevocable = "IDENTITY_CONFLICT_005";
}
