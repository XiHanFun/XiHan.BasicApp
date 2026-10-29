// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 第三方账号绑定。
/// </summary>
/// <remarks>
/// 平台库，跟着账号走。在（注册地, 提供商, 第三方标识）上唯一（4.x 同按租户唯一，但绑定写在注册地、登录却只在租户 1 里查，平台与其他租户的绑定永远登录不进）。
/// 提供商存规范的类型名（<c>github</c>、<c>wechat</c>…，4.x 存配置的方案名，同一个微信 unionid 经两个方案会被当成两个身份）。不存第三方的令牌。
/// </remarks>
[DiffLogIgnored]
public sealed class SysUserExternalLogin : AppAuditedEntity
{
    /// <summary>
    /// 用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户的注册地（平台账号为 <c>platform</c>）。
    /// </summary>
    [StringLength(64)]
    public string HomeTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 提供商类型。
    /// </summary>
    [StringLength(32)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 第三方的用户标识。
    /// </summary>
    [StringLength(200)]
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 第三方的显示名（每次登录刷新）。
    /// </summary>
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 第三方的邮箱（每次登录刷新）。
    /// </summary>
    [StringLength(200)]
    public string? Email { get; set; }

    /// <summary>
    /// 第三方的头像（每次登录刷新）。
    /// </summary>
    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 最近一次用它登录的时间。
    /// </summary>
    /// <remarks>
    /// 4.x 只在绑定时写一次。
    /// </remarks>
    public DateTimeOffset? LastLoginAt { get; set; }
}
