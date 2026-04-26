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

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统会话角色映射实体
/// 记录会话运行期"当前激活"的角色子集；SysUserRole 是"持有"，本表是"激活"，用于动态职责分离 DSD
/// </summary>
/// <remarks>
/// 关联：
/// - SessionId → SysUserSession；RoleId → SysRole
///
/// 写入：
/// - SessionId + RoleId 唯一（UX_SeId_RoId）
/// - 激活前必须校验：该用户确实持有此角色（SysUserRole 存在且未过期）
/// - 激活时必须通过 DSD 约束检查（SysConstraintRule 中 ConstraintType=DSD）
/// - ExpiresAt 可为空（随会话结束失效）或设定具体到期时间
///
/// 查询：
/// - 会话鉴权：按 SessionId 查当前激活角色集（IX_SeId）
/// - 角色反查活跃会话：IX_RoId
/// - 过期清理：按 IX_ExAt 定时扫描
///
/// 删除：
/// - 硬删；或将 Status 置为 Inactive 保留审计
///
/// 状态：
/// - Status (SessionRoleStatus): Active=激活中 / Inactive=已停用 / Expired=已过期
/// - ActivatedAt / DeactivatedAt / ExpiresAt 记录生命周期
///
/// 场景：
/// - 用户持有多角色但每次会话仅激活其一（避免 SoD 冲突）
/// - 敏感操作前临时激活特权角色，完成后立即 Deactivate
/// - 凌晨自动扫描并失效 ExpiresAt 已过的会话角色
/// </remarks>
[SugarTable("SysSessionRole", "系统会话角色映射表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_SeId_RoId", nameof(TenantId), OrderByType.Asc, nameof(SessionId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_SeId", nameof(SessionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_RoId", nameof(RoleId), OrderByType.Asc)]
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
    public virtual DateTimeOffset ActivatedAt { get; set; }

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
