#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSessionRole
// Guid:5d6e7f89-0123-4567-ef01-345678901234
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统会话角色映射实体
/// 记录会话中激活的角色，支持动态职责分离（DSD）
/// </summary>
[SugarTable("Sys_Session_Role", "系统会话角色映射表")]
[SugarIndex("UX_SysSessionRole_SeId_RoId", nameof(SessionId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc, true)]
[SugarIndex("IX_SysSessionRole_SeId", nameof(SessionId), OrderByType.Asc)]
[SugarIndex("IX_SysSessionRole_RoId", nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_SysSessionRole_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysSessionRole_ExAt", nameof(ExpiresAt), OrderByType.Asc)]
[SugarIndex("IX_SysSessionRole_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysSessionRole : BasicAppCreationEntity
{
    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "会话ID", IsNullable = false)]
    public virtual long SessionId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", IsNullable = false)]
    public virtual long RoleId { get; set; }

    /// <summary>
    /// 激活时间
    /// </summary>
    [SugarColumn(ColumnDescription = "激活时间")]
    public virtual DateTimeOffset ActivatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 停用时间
    /// </summary>
    [SugarColumn(ColumnDescription = "停用时间", IsNullable = true)]
    public virtual DateTimeOffset? DeactivatedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 会话角色状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual SessionRoleStatus Status { get; set; } = SessionRoleStatus.Active;

    /// <summary>
    /// 激活原因/备注
    /// </summary>
    [SugarColumn(ColumnDescription = "激活原因", Length = 500, IsNullable = true)]
    public virtual string? Reason { get; set; }
}
