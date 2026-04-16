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
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统角色自定义数据权限范围实体
/// 用于配置角色的自定义数据权限规则（当DataScope=Custom时使用）
/// </summary>
[SugarTable("SysRoleDataScope", "系统角色自定义数据权限范围表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_RoId_DeId", nameof(RoleId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_DeId", nameof(DepartmentId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_RoId", nameof(TenantId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
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
    /// 是否包含子部门（true 时自动包含该部门的所有下级，新增子部门自动纳入范围）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否包含子部门")]
    public virtual bool IncludeChildren { get; set; } = false;

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
