#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePermission
// Guid:8c28152c-d6e9-4396-addb-b479254bad12
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 2:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Entities.Base;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 角色权限关联实体
/// </summary>
[SugarTable("sys_role_permission", "角色权限关联表")]
public partial class SysRolePermission : RbacFullAuditedEntity<RbacIdType>
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
