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
}
