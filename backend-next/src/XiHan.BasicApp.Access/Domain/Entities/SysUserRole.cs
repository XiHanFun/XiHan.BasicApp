// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 用户与角色的指派（平台库实体）。
/// </summary>
/// <remarks>
/// <see cref="OwnerTenantId"/> 与角色相同，用户的注册地必须是同一侧。
/// 可带生效与到期时间：不在有效期内的指派不进令牌，也不参与判定（令牌里已有的角色到期即由权限过滤器拿掉）。
/// </remarks>
public sealed class SysUserRole : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 用户 ID。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 角色 ID。
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 生效时间。
    /// </summary>
    /// <remarks>
    /// 为空即立即生效。
    /// </remarks>
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 到期时间（不含）。
    /// </summary>
    /// <remarks>
    /// 为空即长期有效。
    /// </remarks>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 此刻是否在有效期内。
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}
