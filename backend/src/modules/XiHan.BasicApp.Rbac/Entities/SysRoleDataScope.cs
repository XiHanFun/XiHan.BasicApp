#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleDataScope
// Guid:5c28152c-d6e9-4396-addb-b479254bad24
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统角色自定义数据权限范围实体
/// 用于配置角色的自定义数据权限规则（当DataScope=Custom时使用）
/// </summary>
[SugarTable("Sys_Role_Data_Scope", "系统角色自定义数据权限范围表")]
[SugarIndex("UX_SysRoleDataScope_RoId_DeId", nameof(RoleId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc, true)]
[SugarIndex("IX_SysRoleDataScope_RoId", nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_SysRoleDataScope_DeId", nameof(DepartmentId), OrderByType.Asc)]
[SugarIndex("IX_SysRoleDataScope_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysRoleDataScope_TeId_RoId", nameof(TenantId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_SysRoleDataScope_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysRoleDataScope : BasicAppCreationEntity
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", IsNullable = false)]
    public virtual long RoleId { get; set; }

    /// <summary>
    /// 部门ID（自定义数据权限可访问的部门）
    /// </summary>
    [SugarColumn(ColumnDescription = "部门ID", IsNullable = false)]
    public virtual long DepartmentId { get; set; }

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
