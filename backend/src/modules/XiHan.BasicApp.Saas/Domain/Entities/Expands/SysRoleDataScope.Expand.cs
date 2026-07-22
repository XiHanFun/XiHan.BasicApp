// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统角色自定义数据权限范围实体扩展
/// </summary>
public partial class SysRoleDataScope
{
    /// <summary>
    /// 角色（多条数据范围可属同一角色，ManyToOne）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(RoleId))]
    public virtual SysRole? Role { get; set; }

    /// <summary>
    /// 部门（多条数据范围可指向同一部门，ManyToOne）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(DepartmentId))]
    public virtual SysDepartment? Department { get; set; }
}
