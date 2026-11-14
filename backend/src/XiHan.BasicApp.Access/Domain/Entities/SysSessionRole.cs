// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 会话激活的角色（平台库实体）
/// </summary>
/// <remarks>
/// 会话 × 角色一行，随会话所在的一侧。令牌只带激活中的角色；
/// 激活前校验持有、激活时过动态职责分离（DSD）。切换租户、重新激活时旧行记停用，不再持有的记到期，留作查询
/// </remarks>
[SugarTable(TableName = "sys_session_role", TableDescription = "会话激活的角色（平台库实体）")]
[SugarIndex("ux_sys_session_role_session_id_owner_tenant_id_role_id", nameof(SessionId), OrderByType.Asc, nameof(OwnerTenantId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc, true)]
[SugarIndex("ix_sys_session_role_owner_tenant_id_activated_at", nameof(OwnerTenantId), OrderByType.Asc, nameof(ActivatedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_session_role_owner_tenant_id_user_id", nameof(OwnerTenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
public sealed class SysSessionRole : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 会话 ID（访问令牌的 <c>sid</c>）
    /// </summary>
    [SugarColumn(ColumnName = "session_id", IsNullable = false, ColumnDescription = "会话 ID（访问令牌的 sid）")]
    public Guid SessionId { get; set; }

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
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public SessionRoleStatus Status { get; set; } = SessionRoleStatus.Active;

    /// <summary>
    /// 激活时间
    /// </summary>
    [SugarColumn(ColumnName = "activated_at", IsNullable = false, ColumnDescription = "激活时间")]
    public DateTimeOffset ActivatedAt { get; set; }

    /// <summary>
    /// 停用或到期的时间
    /// </summary>
    [SugarColumn(ColumnName = "deactivated_at", IsNullable = true, ColumnDescription = "停用或到期的时间")]
    public DateTimeOffset? DeactivatedAt { get; set; }

    /// <summary>
    /// 激活、停用或到期的原因
    /// </summary>
    /// <remarks>
    /// 激活时为 <c>login</c>、<c>switch-tenant</c>、<c>impersonation</c>、<c>refresh</c> 或 <c>manual</c>；停用时为 <c>manual</c>、<c>switch-tenant</c>、<c>dsd:&lt;规则编码&gt;</c>，到期为 <c>not-held</c>
    /// </remarks>
    [SugarColumn(ColumnName = "reason", Length = 500, IsNullable = true, ColumnDescription = "激活、停用或到期的原因")]
    public string? Reason { get; set; }
}

/// <summary>
/// 会话角色的状态
/// </summary>
public enum SessionRoleStatus
{
    /// <summary>
    /// 激活中
    /// </summary>
    Active = 0,

    /// <summary>
    /// 已停用（本人换了激活集、切换租户或续签时按 DSD 停掉）
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// 已到期（不再持有这个角色）
    /// </summary>
    Expired = 2,
}
