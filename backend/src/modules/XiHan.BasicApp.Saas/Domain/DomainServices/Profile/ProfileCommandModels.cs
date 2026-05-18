#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileCommandModels
// Guid:0187efe8-dad8-45ea-87fe-a4c41af8df42
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 个人中心联系方式类型
/// </summary>
public enum ProfileContactKind
{
    /// <summary>
    /// 邮箱
    /// </summary>
    Email = 1,

    /// <summary>
    /// 手机号
    /// </summary>
    Phone = 2
}

/// <summary>
/// 个人资料更新命令
/// </summary>
public sealed record ProfileUpdateCommand(
    long UserId,
    string? NickName,
    string? RealName,
    string? Avatar,
    int? Gender,
    DateTimeOffset? Birthday,
    string? TimeZone,
    string? Language,
    string? Country,
    string? Remark);

/// <summary>
/// 当前用户密码修改命令
/// </summary>
public sealed record ProfileChangePasswordCommand(long UserId, long InputUserId, string? OldPassword, string? NewPassword);

/// <summary>
/// 当前用户名修改命令
/// </summary>
public sealed record ProfileChangeUserNameCommand(long UserId, string? UserName, string? Password);

/// <summary>
/// 准备联系方式换绑命令
/// </summary>
public sealed record ProfileChangeContactPrepareCommand(long UserId, ProfileContactKind ContactKind, string? Target, string? Password);

/// <summary>
/// 确认联系方式换绑命令
/// </summary>
public sealed record ProfileConfirmContactCommand(long UserId, ProfileContactKind ContactKind, string? Target);

/// <summary>
/// 验证当前联系方式命令
/// </summary>
public sealed record ProfileVerifyContactCommand(long UserId, ProfileContactKind ContactKind);

/// <summary>
/// 初始化 TOTP 双因素认证命令
/// </summary>
public sealed record ProfileTwoFactorSetupCommand(long UserId, string Issuer);

/// <summary>
/// 双因素认证方式变更命令
/// </summary>
public sealed record ProfileTwoFactorCommand(long UserId, TwoFactorMethod Method);

/// <summary>
/// 当前用户会话撤销命令
/// </summary>
public sealed record ProfileSessionRevokeCommand(long UserId, string? SessionId, string? CurrentSessionId, long? OperatorUserId);

/// <summary>
/// 当前用户其他会话撤销命令
/// </summary>
public sealed record ProfileOtherSessionsRevokeCommand(long UserId, string? CurrentSessionId, long? OperatorUserId);

/// <summary>
/// 第三方账号解绑命令
/// </summary>
public sealed record ProfileUnlinkAccountCommand(long UserId, string? Provider);

/// <summary>
/// 密码确认命令
/// </summary>
public sealed record ProfilePasswordConfirmCommand(long UserId, string? Password, long? OperatorUserId);

/// <summary>
/// 当前用户安全命令结果
/// </summary>
public sealed record ProfileUserSecurityResult(SysUser User, SysUserSecurity Security);

/// <summary>
/// 联系方式换绑准备结果
/// </summary>
public sealed record ProfileContactPrepareResult(SysUser User, string Target);

/// <summary>
/// TOTP 双因素初始化结果
/// </summary>
public sealed record ProfileTwoFactorSetupResult(string SharedKey, string AuthenticatorUri);

/// <summary>
/// 当前用户会话撤销结果
/// </summary>
public sealed record ProfileSessionRevokeResult(IReadOnlyList<UserSessionRevokedDomainEvent> DomainEvents);
