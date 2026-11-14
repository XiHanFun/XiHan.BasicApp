// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 限时委托（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 委托人在有效期内把一个权限码或一个角色交给被委托人（方案 5.9）：临时代办不必改角色指派或直授，到期自动收回
/// 状态不落库，按撤销时间与有效期推出（<see cref="StatusAt"/>）：到点生效与到期不需要谁去改状态，状态也不会与时间对不上；撤销只记撤销时间与撤销人，记录保留
/// 判定时委托人此刻仍须持有所委托的权限或角色，否则不算数：委托人自己被收回了，转出去的随之失效，委托不会比来源更持久
/// 委托不写框架的授予存储，由 <c>AccessGrantSource</c> 在存储没命中时给出，之后照常经权限过滤器收窄；委托的角色按它的继承链给授予，不带数据范围与字段安全，约束规则把它算作被委托人持有的角色
/// 落平台库，只在所属一侧（<see cref="OwnerTenantId"/>）算数；判定读被委托人名下未撤销、未到期的委托，放共享缓存 60 秒，索引按这一读法建
/// 不加唯一约束：只拦同一委托人、被委托人与目标上未撤销、未到期的重复，撤销或到期后可以重建
/// </remarks>
[SugarTable(TableName = "sys_permission_delegation", TableDescription = "限时委托（平台库实体，随所属侧）")]
[SugarIndex("ix_sys_permission_delegation_owner_delegatee_revoked", nameof(OwnerTenantId), OrderByType.Asc, nameof(DelegateeId), OrderByType.Asc, nameof(RevokedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_permission_delegation_owner_delegator_delegatee", nameof(OwnerTenantId), OrderByType.Asc, nameof(DelegatorId), OrderByType.Asc, nameof(DelegateeId), OrderByType.Asc)]
[SugarIndex("ix_sys_permission_delegation_role_id", nameof(RoleId), OrderByType.Asc)]
[SugarIndex("ix_sys_permission_delegation_owner_tenant_id_created_at", nameof(OwnerTenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysPermissionDelegation : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 委托人
    /// </summary>
    [SugarColumn(ColumnName = "delegator_id", IsNullable = false, ColumnDescription = "委托人")]
    public Guid DelegatorId { get; set; }

    /// <summary>
    /// 被委托人
    /// </summary>
    [SugarColumn(ColumnName = "delegatee_id", IsNullable = false, ColumnDescription = "被委托人")]
    public Guid DelegateeId { get; set; }

    /// <summary>
    /// 委托的权限码（与 <see cref="RoleId"/> 二选一）
    /// </summary>
    [SugarColumn(ColumnName = "permission", Length = 128, IsNullable = true, ColumnDescription = "委托的权限码（与 RoleId 二选一）")]
    public string? Permission { get; set; }

    /// <summary>
    /// 委托的角色（与 <see cref="Permission"/> 二选一）
    /// </summary>
    /// <remarks>
    /// 被委托人按这个角色的继承链拿到授予，不拿数据范围与字段安全
    /// </remarks>
    [SugarColumn(ColumnName = "role_id", IsNullable = true, ColumnDescription = "委托的角色（与 Permission 二选一）")]
    public Guid? RoleId { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    /// <remarks>
    /// 为空即创建即生效
    /// </remarks>
    [SugarColumn(ColumnName = "effective_at", IsNullable = true, ColumnDescription = "生效时间")]
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 到期时间（必填）
    /// </summary>
    [SugarColumn(ColumnName = "expires_at", IsNullable = false, ColumnDescription = "到期时间（必填）")]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 委托原因
    /// </summary>
    [SugarColumn(ColumnName = "reason", Length = 512, IsNullable = true, ColumnDescription = "委托原因")]
    public string? Reason { get; set; }

    /// <summary>
    /// 撤销时间
    /// </summary>
    /// <remarks>
    /// 为空即未撤销
    /// </remarks>
    [SugarColumn(ColumnName = "revoked_at", IsNullable = true, ColumnDescription = "撤销时间")]
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// 撤销人
    /// </summary>
    [SugarColumn(ColumnName = "revoked_by", IsNullable = true, ColumnDescription = "撤销人")]
    public Guid? RevokedBy { get; set; }

    /// <summary>
    /// 某一时刻的状态
    /// </summary>
    public DelegationStatus StatusAt(DateTimeOffset now)
        => RevokedAt.HasValue ? DelegationStatus.Revoked
            : ExpiresAt <= now ? DelegationStatus.Expired
            : EffectiveAt is { } from && from > now ? DelegationStatus.Pending
            : DelegationStatus.Active;
}

/// <summary>
/// 委托状态（由撤销时间与有效期推出，不落库）
/// </summary>
public enum DelegationStatus
{
    /// <summary>
    /// 未到生效时间
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 有效期内
    /// </summary>
    Active = 1,

    /// <summary>
    /// 已过到期时间
    /// </summary>
    Expired = 2,

    /// <summary>
    /// 已撤销
    /// </summary>
    Revoked = 3,
}
