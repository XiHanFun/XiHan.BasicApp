#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermission
// Guid:4c28152c-d6e9-4396-addb-b479254bad0e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 2:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统权限实体
/// </summary>
[SugarTable("Sys_Permission", "系统权限表")]
[SugarIndex("IX_SysPermission_PermissionCode", nameof(PermissionCode), OrderByType.Asc, true)]
public partial class SysPermission : RbacFullAuditedEntity<long>
{
    /// <summary>
    /// 权限编码
    /// </summary>
    [SugarColumn(ColumnDescription = "权限编码", Length = 100, IsNullable = false)]
    public virtual string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 权限名称
    /// </summary>
    [SugarColumn(ColumnDescription = "权限名称", Length = 100, IsNullable = false)]
    public virtual string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    [SugarColumn(ColumnDescription = "权限描述", Length = 500, IsNullable = true)]
    public virtual string? PermissionDescription { get; set; }

    /// <summary>
    /// 权限类型
    /// </summary>
    [SugarColumn(ColumnDescription = "权限类型")]
    public virtual PermissionType PermissionType { get; set; } = PermissionType.Menu;

    /// <summary>
    /// 权限值
    /// </summary>
    [SugarColumn(ColumnDescription = "权限值", Length = 200, IsNullable = true)]
    public virtual string? PermissionValue { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
