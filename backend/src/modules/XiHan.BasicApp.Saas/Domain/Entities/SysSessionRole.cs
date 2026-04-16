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
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统会话角色映射实体
/// 记录会话中激活的角色，支持动态职责分离（DSD）
/// </summary>
[SugarTable("SysSessionRole", "系统会话角色映射表")]
[SugarIndex("IX_{table}_TeId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CrTi", nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_SeId_RoId", nameof(SessionId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_SeId", nameof(SessionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_RoId", nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_{table}_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_ExAt", nameof(ExpiresAt), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
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
