// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统角色继承边实体扩展
/// </summary>
public partial class SysRoleHierarchy : IValidatableObject
{
    /// <summary>
    /// 上级角色
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(AncestorId))]
    public virtual SysRole? Ancestor { get; set; }

    /// <summary>
    /// 下级角色
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(DescendantId))]
    public virtual SysRole? Descendant { get; set; }

    /// <summary>
    /// 校验实体自身的业务规则
    /// </summary>
    /// <param name="validationContext">校验上下文</param>
    /// <returns>校验失败项集合，全部通过时为空集合</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AncestorId <= 0 || DescendantId <= 0)
        {
            yield return new ValidationResult("上级角色与下级角色都必须指定。", [nameof(AncestorId), nameof(DescendantId)]);
        }

        if (AncestorId == DescendantId)
        {
            yield return new ValidationResult("角色不能继承自己。", [nameof(AncestorId), nameof(DescendantId)]);
        }
    }
}
