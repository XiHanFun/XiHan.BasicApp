// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 角色权限关联实体扩展
/// </summary>
public partial class SysRolePermission
{
    /// <summary>
    /// 角色信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(RoleId))]
    public virtual SysRole? Role { get; set; }

    /// <summary>
    /// 权限信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(PermissionId))]
    public virtual SysPermission? Permission { get; set; }

    /// <summary>
    /// ABAC 条件列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysPermissionCondition.RolePermissionId))]
    public virtual List<SysPermissionCondition>? Conditions { get; set; }
}
