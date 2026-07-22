// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统角色层级关系实体扩展
/// </summary>
public partial class SysRoleHierarchy : IValidatableObject
{
    /// <summary>
    /// 祖先角色
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(AncestorId))]
    public virtual SysRole? Ancestor { get; set; }

    /// <summary>
    /// 后代角色
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(DescendantId))]
    public virtual SysRole? Descendant { get; set; }

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
