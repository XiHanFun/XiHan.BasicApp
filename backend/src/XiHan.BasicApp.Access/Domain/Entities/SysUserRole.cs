// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 用户与角色的指派（平台库实体）
/// </summary>
/// <remarks>
/// <see cref="OwnerTenantId"/> 与角色相同，用户的注册地必须是同一侧
/// 可带生效与到期时间：不在有效期内的指派不进令牌，也不参与判定（令牌里已有的角色到期即由权限过滤器拿掉）
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
