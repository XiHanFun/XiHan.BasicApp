#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartmentHierarchy.pl
// Guid:19ab7109-4827-46e3-9e2b-f49adf7eb789
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统部门继承关系实体扩展
/// </summary>
public partial class SysDepartmentHierarchy
{
    /// <summary>
    /// 祖先部门
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(AncestorId))]
    public virtual SysDepartment? Ancestor { get; set; }

    /// <summary>
    /// 后代部门
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(DescendantId))]
    public virtual SysDepartment? Descendant { get; set; }
}
