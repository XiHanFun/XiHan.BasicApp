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
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统部门继承关系实体扩展
/// </summary>
public partial class SysDepartmentHierarchy : IValidatableObject
{
    /// <summary>
    /// 祖先部门（多条层级记录可指向同一部门，ManyToOne）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(AncestorId))]
    public virtual SysDepartment? Ancestor { get; set; }

    /// <summary>
    /// 后代部门（多条层级记录可指向同一部门，ManyToOne）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(DescendantId))]
    public virtual SysDepartment? Descendant { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Depth < 0)
        {
            yield return new ValidationResult("Depth 不能为负数。", [nameof(Depth)]);
        }

        if (Depth == 0 && AncestorId != DescendantId)
        {
            yield return new ValidationResult("Depth=0 时 AncestorId 必须等于 DescendantId（自环记录）。",
                [nameof(Depth), nameof(AncestorId), nameof(DescendantId)]);
        }

        if (Depth > 0 && AncestorId == DescendantId)
        {
            yield return new ValidationResult("Depth>0 时 AncestorId 不能等于 DescendantId。",
                [nameof(Depth), nameof(AncestorId), nameof(DescendantId)]);
        }
    }
}
