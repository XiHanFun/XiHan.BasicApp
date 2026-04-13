#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionCondition.pl
// Guid:d0e1f2a3-b4c5-6789-3456-890123456710
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限 ABAC 条件实体扩展
/// </summary>
public partial class SysPermissionCondition
{
    /// <summary>
    /// 关联的角色权限
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(RolePermissionId))]
    public virtual SysRolePermission? RolePermission { get; set; }

    /// <summary>
    /// 关联的用户直授权限
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(UserPermissionId))]
    public virtual SysUserPermission? UserPermission { get; set; }
}
