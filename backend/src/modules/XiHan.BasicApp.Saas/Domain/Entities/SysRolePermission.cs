#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePermission
// Guid:8c28152c-d6e9-4396-addb-b479254bad12
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统角色权限关联实体
/// </summary>
/// <remarks>
/// 权限合并优先级（从高到低）：
/// 1. 用户直授（SysUserPermission）的 Deny → 最终拒绝，不可被任何角色 Grant 覆盖
/// 2. 用户直授（SysUserPermission）的 Grant → 最终授予，即使所有角色 Deny
/// 3. 角色级 Deny（本表）→ 仅作用于当前角色的继承链，不影响其他独立角色
/// 4. 角色级 Grant（本表）→ 当前角色的权限授予
///
/// 角色级 Deny 语义：若角色 B 继承角色 A，B 对权限 P 标记 Deny，
/// 则通过 B 不会获得 P；但用户若同时直接持有角色 A，仍可通过 A 获得 P。
/// </remarks>
[SugarTable("Sys_Role_Permission", "系统角色权限关联表")]
[SugarIndex("UX_SysRolePermission_RoId_PeId", nameof(RoleId), OrderByType.Asc, nameof(PermissionId), OrderByType.Asc, true)]
[SugarIndex("IX_SysRolePermission_RoId", nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_SysRolePermission_PeId", nameof(PermissionId), OrderByType.Asc)]
[SugarIndex("IX_SysRolePermission_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysRolePermission_TeId_RoId", nameof(TenantId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_SysRolePermission_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysRolePermission_PeAc", nameof(PermissionAction), OrderByType.Asc)]
public partial class SysRolePermission : BasicAppCreationEntity
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", IsNullable = false)]
    public virtual long RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    [SugarColumn(ColumnDescription = "权限ID", IsNullable = false)]
    public virtual long PermissionId { get; set; }

    /// <summary>
    /// 权限操作（授予/禁用，支持角色继承时覆盖父角色权限）
    /// </summary>
    [SugarColumn(ColumnDescription = "权限操作")]
    public virtual PermissionAction PermissionAction { get; set; } = PermissionAction.Grant;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
