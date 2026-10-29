// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 账号安全信息：密码散列、失败计数与锁定、安全戳与双因素。
/// </summary>
/// <remarks>
/// 平台库，表 <c>sys_user_security</c>，与用户一对一。与用户资料分开，列表与详情永远碰不到散列与密钥。
/// </remarks>
[DiffLogIgnored]
public sealed class SysUserSecurity : AppAuditedEntity
{
    /// <summary>
    /// 用户 ID（唯一）。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 密码的 PBKDF2 散列。
    /// </summary>
    /// <remarks>
    /// 格式 <c>pbkdf2-sha256$迭代$盐$散列</c>。空串表示尚未设置密码，不能登录。
    /// </remarks>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 最近一次改密时间。
    /// </summary>
    public DateTimeOffset? PasswordChangedAt { get; set; }

    /// <summary>
    /// 最近一次本人改用户名的时间。
    /// </summary>
    /// <remarks>
    /// 90 天一次，4.x 同。
    /// </remarks>
    public DateTimeOffset? UserNameChangedAt { get; set; }

    /// <summary>
    /// 自上次成功登录以来的连续失败次数。
    /// </summary>
    public int FailedAccessCount { get; set; }

    /// <summary>
    /// 锁定截止时间。
    /// </summary>
    /// <remarks>
    /// 之前的登录一律拒绝。
    /// </remarks>
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
    /// 是否开启了双因素。
    /// </summary>
    /// <remarks>
    /// 开启后密码通过还要过第二步（<see cref="TwoFactorMethods"/> 里至少有一种）。
    /// </remarks>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// 开启的双因素方式（按位组合）。
    /// </summary>
    public TwoFactorMethods TwoFactorMethods { get; set; }

    /// <summary>
    /// 认证器密钥。
    /// </summary>
    /// <remarks>
    /// Base32，经 <c>ISecretProtector</c> 加密后存。没开认证器为空。
    /// </remarks>
    [StringLength(1024)]
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// 认证器最近一次通过的时间步：同一步（及更早的）验证码不能再用，防重放。
    /// </summary>
    public long? TwoFactorLastStep { get; set; }

    /// <summary>
    /// 开放接口的签名算法。
    /// </summary>
    /// <remarks>
    /// 个人中心「调用安全设置」，作用于本人全部凭证。为空即 HMAC-SHA256。
    /// </remarks>
    [StringLength(32)]
    public string? ApiSignatureAlgorithm { get; set; }

    /// <summary>
    /// 开放接口的 IP 白名单。
    /// </summary>
    /// <remarks>
    /// 每行一个 IP 或 CIDR，作用于本人全部凭证。为空即不限。
    /// </remarks>
    [StringLength(2048)]
    public string? ApiIpAllowList { get; set; }
}
