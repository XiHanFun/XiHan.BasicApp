#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRole
// Guid:3c28152c-d6e9-4396-addb-b479254bad0d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Domain.Entities;

/// <summary>
/// 系统角色实体
/// </summary>
[SugarTable("Sys_Role", "系统角色表")]
[SugarIndex("IX_SysRole_RoCo", nameof(RoleCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysRole_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysRole_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysRole : BasicAppAggregateRoot
{
    /// <summary>
    /// 角色编码
    /// </summary>
    [SugarColumn(ColumnDescription = "角色编码", Length = 50, IsNullable = false)]
    public virtual string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 角色名称
    /// </summary>
    [SugarColumn(ColumnDescription = "角色名称", Length = 100, IsNullable = false)]
    public virtual string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    [SugarColumn(ColumnDescription = "角色描述", Length = 500, IsNullable = true)]
    public virtual string? RoleDescription { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    [SugarColumn(ColumnDescription = "角色类型")]
    public virtual RoleType RoleType { get; set; } = RoleType.System;

    /// <summary>
    /// 数据权限范围
    /// </summary>
    [SugarColumn(ColumnDescription = "数据权限范围")]
    public virtual DataPermissionScope DataScope { get; set; } = DataPermissionScope.SelfOnly;

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
