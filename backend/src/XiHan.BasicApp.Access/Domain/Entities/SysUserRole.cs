// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 用户与角色的指派（平台库实体）
/// </summary>
/// <remarks>
/// <see cref="OwnerTenantId"/> 与角色相同，按侧读一个用户的指派就是他在这一侧的全部角色；用户须在这一侧：注册在这里的账号，或这个租户生效中的外部成员与运维支持成员
/// 可带生效与到期时间：不在有效期内的指派不进令牌，也不参与判定。令牌里的角色是签发那一刻的快照，权限过滤器拿它与此刻的指派求交，所以到期与解除不等令牌过期就不再算数；新指派最迟在下一次刷新令牌时进令牌
/// 一个用户在一侧的指派放共享缓存 60 秒，指派变化时失效；角色停用不失效这份缓存，经缓存存活期传到判定
/// 给角色指派成员只有一套规则（同侧、约束规则、权限变更日志、缓存失效），管理员指派、导入用户与授权申请审批通过都经它
/// 解除即物理删除，同一角色同一用户只有一行（普通唯一索引）；成员被移出租户时解除他在这个租户的全部角色，删角色时连同删掉
/// </remarks>
[SugarTable(TableName = "sys_user_role", TableDescription = "用户与角色的指派（平台库实体）")]
[SugarIndex("ux_sys_user_role_role_id_user_id", nameof(RoleId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, true)]
[SugarIndex("ix_sys_user_role_owner_tenant_id_user_id", nameof(OwnerTenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
public sealed class SysUserRole : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 用户 ID
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户 ID")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 角色 ID
    /// </summary>
    [SugarColumn(ColumnName = "role_id", IsNullable = false, ColumnDescription = "角色 ID")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    /// <remarks>
    /// 为空即立即生效
    /// </remarks>
    [SugarColumn(ColumnName = "effective_at", IsNullable = true, ColumnDescription = "生效时间")]
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 到期时间（不含）
    /// </summary>
    /// <remarks>
    /// 为空即长期有效
    /// </remarks>
    [SugarColumn(ColumnName = "expires_at", IsNullable = true, ColumnDescription = "到期时间（不含）")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 此刻是否在有效期内
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}
