// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 限时委托（平台库实体，随所属侧）。
/// </summary>
/// <remarks>
/// 委托人在有效期内把一个权限码或一个角色交给被委托人（方案 5.9）。
/// 状态不落库，按撤销时间与有效期推出（<see cref="StatusAt"/>）：到了生效时间自动生效，过了到期时间自动失效；撤销保留记录。
/// 判定时委托人此刻仍须持有所委托的权限或角色，否则不算数。
/// </remarks>
public sealed class SysPermissionDelegation : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 委托人。
    /// </summary>
    public Guid DelegatorId { get; set; }

    /// <summary>
    /// 被委托人。
    /// </summary>
    public Guid DelegateeId { get; set; }

    /// <summary>
    /// 委托的权限码（与 <see cref="RoleId"/> 二选一）。
    /// </summary>
    [StringLength(128)]
    public string? Permission { get; set; }

    /// <summary>
    /// 委托的角色（与 <see cref="Permission"/> 二选一）。
    /// </summary>
    /// <remarks>
    /// 被委托人按这个角色的继承链拿到授予，不拿数据范围与字段安全。
    /// </remarks>
    public Guid? RoleId { get; set; }

    /// <summary>
    /// 生效时间。
    /// </summary>
    /// <remarks>
    /// 为空即创建即生效。
    /// </remarks>
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 到期时间（必填）。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 委托原因。
    /// </summary>
    [StringLength(512)]
    public string? Reason { get; set; }

    /// <summary>
    /// 撤销时间。
    /// </summary>
    /// <remarks>
    /// 为空即未撤销。
    /// </remarks>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// 撤销人。
    /// </summary>
    public Guid? RevokedBy { get; set; }

    /// <summary>
    /// 某一时刻的状态。
    /// </summary>
    public DelegationStatus StatusAt(DateTimeOffset now)
        => RevokedAt.HasValue ? DelegationStatus.Revoked
            : ExpiresAt <= now ? DelegationStatus.Expired
            : EffectiveAt is { } from && from > now ? DelegationStatus.Pending
            : DelegationStatus.Active;
}

/// <summary>
/// 委托状态（由撤销时间与有效期推出，不落库）。
/// </summary>
public enum DelegationStatus
{
    /// <summary>
    /// 未到生效时间。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 有效期内。
    /// </summary>
    Active = 1,

    /// <summary>
    /// 已过到期时间。
    /// </summary>
    Expired = 2,

    /// <summary>
    /// 已撤销。
    /// </summary>
    Revoked = 3,
}
