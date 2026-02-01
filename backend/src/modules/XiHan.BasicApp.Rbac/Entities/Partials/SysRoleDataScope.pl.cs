#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleDataScope.pl
// Guid:6c28152c-d6e9-4396-addb-b479254bad25
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统角色自定义数据权限范围实体扩展
/// </summary>
public partial class SysRoleDataScope
{
    /// <summary>
    /// 角色
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(RoleId))]
    public virtual SysRole? Role { get; set; }

    /// <summary>
    /// 部门
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(DepartmentId))]
    public virtual SysDepartment? Department { get; set; }
}
