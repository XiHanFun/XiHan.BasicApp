// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 会话激活的角色（平台库实体）
/// </summary>
/// <remarks>
/// 会话 × 角色一行：动态职责分离（DSD）约束的是同一会话同时激活的角色，持有关系在 <see cref="SysUserRole"/>，激活集要另记；令牌只带激活中的角色
/// 落平台库，<see cref="OwnerTenantId"/> 随会话所在的一侧；同一会话同一侧同一角色只有一行，再激活即原地改回激活中，停用与到期也只改状态、不删行，留作「这个会话当时用着哪些角色」的查询
/// 只由 Identity 在签发令牌处经 Contracts 的 <c>ISessionRoleActivator</c> 写：登录、切换租户与开始模仿时重新来过，续签时不再持有的记到期，本人或 DSD 停掉的不自动恢复；激活前校验持有、激活时过 DSD
/// 有角色被 DSD 停掉或不再持有时另记一条登录日志：用户少了角色时查得到原因
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
    /// 签发令牌时激活与停用记调用的操作 ID（如 <c>auth.login</c>、<c>auth.switch-tenant</c>，管道外为 <c>internal</c>）；手动激活与停用为 <c>manual</c>，被职责分离挡下为 <c>dsd:&lt;规则编码&gt;</c>，不再持有即到期为 <c>not-held</c>
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
