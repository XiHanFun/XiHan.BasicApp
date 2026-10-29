// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;

namespace XiHan.BasicApp.Access.Operations.Delegations;

/// <summary>
/// <c>delegations.create</c> 的请求：在当前一侧新建委托。
/// </summary>
/// <param name="DelegatorId">委托人：这一侧的用户，此刻须持有所委托的权限或角色。</param>
/// <param name="DelegateeId">被委托人：这一侧的另一个用户。</param>
/// <param name="ExpiresAt">到期时间（必填，晚于现在与生效时间）。</param>
/// <param name="Permission">委托的权限码（与 <paramref name="RoleId"/> 二选一）。</param>
/// <param name="RoleId">委托的角色（启用的非内置角色）。</param>
/// <param name="EffectiveAt">生效时间；为空即立即生效。</param>
/// <param name="Reason">委托原因。</param>
public sealed record CreateDelegationRequest(
    [Required] Guid DelegatorId,
    [Required] Guid DelegateeId,
    [Required] DateTimeOffset ExpiresAt,
    [StringLength(128)] string? Permission = null,
    Guid? RoleId = null,
    DateTimeOffset? EffectiveAt = null,
    [StringLength(DelegationService.MaxReasonLength)] string? Reason = null)
{
    /// <summary>
    /// 转成委托定义。
    /// </summary>
    public DelegationDefinition ToDefinition() => new(DelegatorId, DelegateeId, Permission, RoleId, EffectiveAt, ExpiresAt, Reason);
}

/// <summary>
/// <c>delegations.revoke</c> 的请求。
/// </summary>
/// <param name="Id">委托 ID。</param>
public sealed record RevokeDelegationRequest([Required] Guid Id);

/// <summary>
/// <c>delegations.delete</c> 的请求。
/// </summary>
/// <param name="Ids">委托 ID，1 到 100 个。</param>
public sealed record DelegationIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// 一条委托。
/// </summary>
/// <param name="Id">委托 ID。</param>
/// <param name="DelegatorId">委托人。</param>
/// <param name="DelegatorName">委托人的显示名（「显示名 (@登录名)」，用户已删除时为空）。</param>
/// <param name="DelegateeId">被委托人。</param>
/// <param name="DelegateeName">被委托人的显示名。</param>
/// <param name="Permission">委托的权限码。</param>
/// <param name="RoleId">委托的角色。</param>
/// <param name="RoleName">委托的角色名。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">到期时间。</param>
/// <param name="Reason">委托原因。</param>
/// <param name="Status">此刻的状态（按撤销时间与有效期推出）。</param>
/// <param name="RevokedAt">撤销时间。</param>
/// <param name="CreatedAt">创建时间。</param>
public sealed record DelegationResponse(
    Guid Id,
    Guid DelegatorId,
    string? DelegatorName,
    Guid DelegateeId,
    string? DelegateeName,
    string? Permission,
    Guid? RoleId,
    string? RoleName,
    DateTimeOffset? EffectiveAt,
    DateTimeOffset ExpiresAt,
    string? Reason,
    DelegationStatus Status,
    DateTimeOffset? RevokedAt,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 由实体与显示名组装。
    /// </summary>
    public static DelegationResponse From(SysPermissionDelegation delegation, DateTimeOffset now, IReadOnlyDictionary<Guid, string> users, IReadOnlyDictionary<Guid, string> roles)
    {
        ArgumentNullException.ThrowIfNull(delegation);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(roles);
        return new(
            delegation.Id,
            delegation.DelegatorId,
            users.GetValueOrDefault(delegation.DelegatorId),
            delegation.DelegateeId,
            users.GetValueOrDefault(delegation.DelegateeId),
            delegation.Permission,
            delegation.RoleId,
            delegation.RoleId is { } roleId ? roles.GetValueOrDefault(roleId) : null,
            delegation.EffectiveAt,
            delegation.ExpiresAt,
            delegation.Reason,
            delegation.StatusAt(now),
            delegation.RevokedAt,
            delegation.CreatedAt);
    }
}
