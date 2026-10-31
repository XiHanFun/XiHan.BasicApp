// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包的错误码。
/// </summary>
/// <remarks>
/// 格式为 <c>IDENTITY_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </remarks>
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
    /// 会话已吊销、已过期或不存在。
    /// </summary>
    /// <remarks>
    /// 客户端应重新登录。
    /// </remarks>
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
    /// 双因素方式在这里不可用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>method</c>。没开、联系方式没验证、票据不许用，或认证器不发码。
    /// </remarks>
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
    /// 按字段安全读受保护的字段筛选或排序。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string FieldProtected = "IDENTITY_DENIED_011";

    /// <summary>
    /// 填写或改动了字段安全里只读的字段。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string FieldReadOnly = "IDENTITY_DENIED_012";

    /// <summary>
    /// 登录失败太频繁，本窗口内暂停尝试。
    /// </summary>
    /// <remarks>
    /// 参数 <c>retryAfter</c> 秒。
    /// </remarks>
    public const string LoginThrottled = "IDENTITY_UNAVAILABLE_001";

    /// <summary>
    /// 会话不存在、已失效或不在当前一侧与数据范围内。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string SessionNotFound = "IDENTITY_NOTFOUND_002";

    /// <summary>
    /// 不能强制下线自己当前的会话（用退出登录）。
    /// </summary>
    public const string CurrentSessionNotRevocable = "IDENTITY_CONFLICT_005";

    /// <summary>
    /// 会话已锁定，只放行解锁与注销。
    /// </summary>
    /// <remarks>
    /// 参数 <c>reason</c>（锁定原因，锁屏为 <c>ScreenLock</c>）。
    /// </remarks>
    public const string SessionLocked = "IDENTITY_DENIED_002";

    /// <summary>
    /// 锁屏密码不能为空（没有密码的锁屏谁都能解开）。
    /// </summary>
    public const string LockPasswordRequired = "IDENTITY_VALIDATION_011";

    /// <summary>
    /// 锁屏密码不对。
    /// </summary>
    /// <remarks>
    /// 参数 <c>remaining</c>（还能试几次）。
    /// </remarks>
    public const string UnlockPasswordWrong = "IDENTITY_DENIED_013";

    /// <summary>
    /// 解锁连错 5 次，会话已吊销。
    /// </summary>
    public const string UnlockExhausted = "IDENTITY_DENIED_014";

    /// <summary>
    /// 会话的锁定原因不是锁屏，不能用锁屏密码解开。
    /// </summary>
    /// <remarks>
    /// 参数 <c>reason</c>。
    /// </remarks>
    public const string LockReasonUnsupported = "IDENTITY_CONFLICT_006";

    /// <summary>
    /// 开放接口凭证到了上限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>max</c>。
    /// </remarks>
    public const string ApiCredentialLimitReached = "IDENTITY_CONFLICT_007";

    /// <summary>
    /// 开放接口凭证不存在或不是本人的。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string ApiCredentialNotFound = "IDENTITY_NOTFOUND_003";

    /// <summary>
    /// 签名算法不认识。
    /// </summary>
    /// <remarks>
    /// 参数 <c>value</c>。
    /// </remarks>
    public const string ApiSignatureAlgorithmInvalid = "IDENTITY_VALIDATION_012";

    /// <summary>
    /// IP 白名单有一条不是 IP 或 CIDR。
    /// </summary>
    /// <remarks>
    /// 参数 <c>value</c>。
    /// </remarks>
    public const string ApiIpAllowListInvalid = "IDENTITY_VALIDATION_013";

    /// <summary>
    /// IP 白名单条数超过上限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>max</c>。
    /// </remarks>
    public const string ApiIpAllowListTooLong = "IDENTITY_VALIDATION_014";

    /// <summary>
    /// 第三方登录的票据不存在、过期、已用过或用途不符。
    /// </summary>
    public const string ExternalTicketInvalid = "IDENTITY_VALIDATION_015";

    /// <summary>
    /// 这个第三方账号在所选注册地没有绑定账号。
    /// </summary>
    /// <remarks>
    /// 参数 <c>provider</c>。
    /// </remarks>
    public const string ExternalLoginNotBound = "IDENTITY_DENIED_015";

    /// <summary>
    /// 这个第三方账号已被同一注册地的别人绑定。
    /// </summary>
    /// <remarks>
    /// 参数 <c>provider</c>。
    /// </remarks>
    public const string ExternalLoginTaken = "IDENTITY_CONFLICT_008";

    /// <summary>
    /// 本人没有绑定这个提供商。
    /// </summary>
    /// <remarks>
    /// 参数 <c>provider</c>。
    /// </remarks>
    public const string ExternalLoginNotFound = "IDENTITY_NOTFOUND_004";

    /// <summary>
    /// 这个第三方登录方式不存在或没有启用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>provider</c>。
    /// </remarks>
    public const string ExternalProviderNotFound = "IDENTITY_NOTFOUND_005";

    /// <summary>
    /// 头像不是 JPEG / PNG 图片（按文件头认）或超过上限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>max</c>（MB）。
    /// </remarks>
    public const string AvatarInvalid = "IDENTITY_VALIDATION_016";

    /// <summary>
    /// 新用户名与现在的相同。
    /// </summary>
    public const string UserNameUnchanged = "IDENTITY_VALIDATION_017";

    /// <summary>
    /// 距上次改用户名不到 90 天。
    /// </summary>
    /// <remarks>
    /// 参数 <c>days</c>（还要等几天）。
    /// </remarks>
    public const string UserNameChangeTooSoon = "IDENTITY_CONFLICT_009";
    /// <summary>
    /// 验证码发得太频繁。
    /// </summary>
    /// <remarks>
    /// 参数 <c>seconds</c>（两次发码的最短间隔）。
    /// </remarks>
    public const string CodeTooFrequent = "IDENTITY_UNAVAILABLE_002";

    /// <summary>
    /// 这个邮箱或手机号今天收码的次数已达上限。
    /// </summary>
    public const string CodeTargetDailyLimit = "IDENTITY_UNAVAILABLE_003";

    /// <summary>
    /// 当前网络今天发码的次数已达上限。
    /// </summary>
    public const string CodeAddressDailyLimit = "IDENTITY_UNAVAILABLE_004";

    /// <summary>
    /// 验证码输错次数过多，暂时锁定。
    /// </summary>
    /// <remarks>
    /// 参数 <c>minutes</c>（锁定分钟数）。
    /// </remarks>
    public const string CodeLocked = "IDENTITY_UNAVAILABLE_005";

    /// <summary>
    /// 没有可用的邮件或短信发送配置。
    /// </summary>
    /// <remarks>
    /// 参数 <c>channel</c>（<c>Email</c> 或 <c>Phone</c>）。
    /// </remarks>
    public const string CodeChannelUnavailable = "IDENTITY_UNAVAILABLE_006";

    /// <summary>
    /// 验证码无效或已过期。
    /// </summary>
    public const string CodeInvalid = "IDENTITY_VALIDATION_018";

    /// <summary>
    /// 当前账号没有绑定要验证的邮箱或手机号。
    /// </summary>
    /// <remarks>
    /// 参数 <c>channel</c>。
    /// </remarks>
    public const string ContactMissing = "IDENTITY_VALIDATION_019";

    /// <summary>
    /// 新的邮箱或手机号格式不对，或与现在的相同。
    /// </summary>
    /// <remarks>
    /// 参数 <c>channel</c>。
    /// </remarks>
    public const string ContactInvalid = "IDENTITY_VALIDATION_020";

    /// <summary>
    /// 邮箱或手机号已被同一租户的别的账号使用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>channel</c>。
    /// </remarks>
    public const string ContactTaken = "IDENTITY_CONFLICT_010";

    /// <summary>
    /// 没有开放这种登录方式。
    /// </summary>
    /// <remarks>
    /// 参数 <c>method</c>（<c>email</c> 或 <c>phone</c>）。
    /// </remarks>
    public const string LoginMethodDisabled = "IDENTITY_DENIED_016";

    /// <summary>
    /// 系统参数里的登录方式写得不对。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>（系统参数的键）。
    /// </remarks>
    public const string LoginMethodsInvalid = "IDENTITY_VALIDATION_021";

    /// <summary>
    /// 登录用的邮箱或手机号格式不对。
    /// </summary>
    /// <remarks>
    /// 参数 <c>channel</c>。
    /// </remarks>
    public const string LoginTargetInvalid = "IDENTITY_VALIDATION_022";

    /// <summary>
    /// 要用作双因素的邮箱或手机号还没验证。
    /// </summary>
    /// <remarks>
    /// 参数 <c>channel</c>。
    /// </remarks>
    public const string ContactUnverified = "IDENTITY_VALIDATION_023";

    /// <summary>
    /// 第二步只剩这次登录收码的那种方式。
    /// </summary>
    /// <remarks>
    /// 参数 <c>method</c>（<c>Email</c> 或 <c>Phone</c>）；改用密码登录。
    /// </remarks>
    public const string TwoFactorSameAsLogin = "IDENTITY_DENIED_017";

    /// <summary>
    /// 用户是某个租户的所有者，不能删除或锁定。
    /// </summary>
    /// <remarks>
    /// 要先转移所有权（<c>tenants.transfer-owner</c>）。
    /// </remarks>
    public const string TenantOwner = "IDENTITY_CONFLICT_011";

    /// <summary>
    /// 账号已停用，不能登录。
    /// </summary>
    public const string AccountDisabled = "IDENTITY_DENIED_018";

    /// <summary>
    /// 用户处于锁定状态，启用不适用。
    /// </summary>
    /// <remarks>
    /// 要先解锁（<c>users.unlock</c>）。
    /// </remarks>
    public const string UserLocked = "IDENTITY_CONFLICT_012";
}
