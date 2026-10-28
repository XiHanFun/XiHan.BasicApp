// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 账号安全（平台库，表 <c>sys_user_security</c>，与用户一对一）：密码散列、失败计数与锁定、安全戳、双因素。与用户资料分开，列表与详情永远碰不到散列与密钥。
/// </summary>
[DiffLogIgnored]
public sealed class SysUserSecurity : AppAuditedEntity
{
    /// <summary>
    /// 用户 ID（唯一）。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// PBKDF2 散列（<c>pbkdf2-sha256$迭代$盐$散列</c>）；空串表示尚未设置密码，不能登录。
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 最近一次改密时间。
    /// </summary>
    public DateTimeOffset? PasswordChangedAt { get; set; }

    /// <summary>
    /// 自上次成功登录以来的连续失败次数。
    /// </summary>
    public int FailedAccessCount { get; set; }

    /// <summary>
    /// 锁定截止时间；之前的登录一律拒绝。
    /// </summary>
    public DateTimeOffset? LockoutEndAt { get; set; }

    /// <summary>
    /// 安全戳：改密、重置、锁定时更新，用于使既有会话失效。
    /// </summary>
    public string SecurityStamp { get; set; } = string.Empty;

    /// <summary>
    /// 下次登录后必须改密（管理员重置、初始密码）。
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>
    /// 是否开启了双因素：开启后密码通过还要过第二步（<see cref="TwoFactorMethods"/> 里至少有一种）。
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// 开启的双因素方式（按位组合）。
    /// </summary>
    public TwoFactorMethods TwoFactorMethods { get; set; }

    /// <summary>
    /// 认证器密钥（Base32，经 <c>ISecretProtector</c> 加密后存）；没开认证器为空。
    /// </summary>
    [StringLength(1024)]
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// 认证器最近一次通过的时间步：同一步（及更早的）验证码不能再用，防重放。
    /// </summary>
    public long? TwoFactorLastStep { get; set; }
}
