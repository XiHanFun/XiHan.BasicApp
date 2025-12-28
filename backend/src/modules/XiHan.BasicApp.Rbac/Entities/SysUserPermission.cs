#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserPermission
// Guid:9c28152c-d6e9-4396-addb-b479254bad13
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 19:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统用户权限关联实体（直授）
/// </summary>
[SugarTable("Sys_User_Permission", "系统用户权限关联表")]
[SugarIndex("IX_SysUserPermission_UserId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysUserPermission_PermissionId", nameof(PermissionId), OrderByType.Asc)]
public partial class SysUserPermission : RbacFullAuditedEntity<XiHanBasicAppIdType>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual XiHanBasicAppIdType UserId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    [SugarColumn(ColumnDescription = "权限ID", IsNullable = false)]
    public virtual XiHanBasicAppIdType PermissionId { get; set; }

    /// <summary>
    /// 权限操作（授予/禁用）
    /// </summary>
    [SugarColumn(ColumnDescription = "权限操作")]
    public virtual PermissionAction PermissionAction { get; set; } = PermissionAction.Grant;

    /// <summary>
    /// 生效时间
    /// </summary>
    [SugarColumn(ColumnDescription = "生效时间", IsNullable = true)]
    public virtual DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    [SugarColumn(ColumnDescription = "失效时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpirationTime { get; set; }

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

