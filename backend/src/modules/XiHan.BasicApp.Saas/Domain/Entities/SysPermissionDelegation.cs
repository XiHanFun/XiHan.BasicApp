#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionDelegation
// Guid:e5f6a7b8-c9d0-1234-0123-567890123456
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Entities.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限委托实体
/// 建模"委托人→被委托人→权限范围→时间窗口"的完整委托链路
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表承载"权限临时委托"（如经理出差时将审批权限委托给副手）
/// - 与 SysUserPermission（直授权限）区别：委托有明确的委托人、被委托人、时间窗口和可撤销性
///
/// 关联：
/// - DelegatorUserId → SysUser（委托人）
/// - DelegateeUserId → SysUser（被委托人）
/// - PermissionId → SysPermission（被委托的权限，可空表示委托全部权限）
/// - RoleId → SysRole（被委托的角色，可空）
///
/// 写入：
/// - TenantId + DelegatorUserId + DelegateeUserId + PermissionId 唯一（UX_TeId_DrId_DeId_PeId）
/// - 委托生效需同时满足：DelegationStatus=Active AND 当前时间在 EffectiveTime~ExpirationTime 范围内
///
/// 查询：
/// - 我委托出去的：IX_DrId + WHERE DelegatorUserId=?
/// - 委托给我的：IX_DeId + WHERE DelegateeUserId=?
/// - 即将过期扫描：IX_ExTi
///
/// 删除：
/// - 仅软删；撤销委托通过 DelegationStatus=Revoked
/// </remarks>
[SugarTable("SysPermissionDelegation", "权限委托表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_DrId_DeId_PeId", nameof(TenantId), OrderByType.Asc, nameof(DelegatorUserId), OrderByType.Asc, nameof(DelegateeUserId), OrderByType.Asc, nameof(PermissionId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_DrId", nameof(DelegatorUserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_DeId", nameof(DelegateeUserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_DeSt", nameof(TenantId), OrderByType.Asc, nameof(DelegationStatus), OrderByType.Asc)]
[SugarIndex("IX_{table}_ExTi", nameof(ExpirationTime), OrderByType.Asc)]
public partial class SysPermissionDelegation : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 委托人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "委托人ID", IsNullable = false)]
    public virtual long DelegatorUserId { get; set; }

    /// <summary>
    /// 被委托人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "被委托人ID", IsNullable = false)]
    public virtual long DelegateeUserId { get; set; }

    /// <summary>
    /// 权限ID（为空表示委托全部权限）
    /// </summary>
    [SugarColumn(ColumnDescription = "权限ID", IsNullable = true)]
    public virtual long? PermissionId { get; set; }

    /// <summary>
    /// 角色ID（为空表示不限角色）
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", IsNullable = true)]
    public virtual long? RoleId { get; set; }

    /// <summary>
    /// 委托状态
    /// </summary>
    [SugarColumn(ColumnDescription = "委托状态")]
    public virtual DelegationStatus DelegationStatus { get; set; } = DelegationStatus.Pending;

    /// <summary>
    /// 生效时间（为空表示立即生效）
    /// </summary>
    [SugarColumn(ColumnDescription = "生效时间", IsNullable = true)]
    public virtual DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间（必填，委托必须有截止时间）
    /// </summary>
    [SugarColumn(ColumnDescription = "失效时间", IsNullable = false)]
    public virtual DateTimeOffset ExpirationTime { get; set; }

    /// <summary>
    /// 委托原因
    /// </summary>
    [SugarColumn(ColumnDescription = "委托原因", Length = 500, IsNullable = true)]
    public virtual string? DelegationReason { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
