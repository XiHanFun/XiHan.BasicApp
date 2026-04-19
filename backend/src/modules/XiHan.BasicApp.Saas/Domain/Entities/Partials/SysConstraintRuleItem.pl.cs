#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConstraintRuleItem.pl
// Guid:c9d0e1f2-a3b4-5678-2345-789012345609
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 约束规则目标项实体扩展
/// </summary>
public partial class SysConstraintRuleItem : IValidatableObject
{
    /// <summary>
    /// 所属约束规则
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(ConstraintRuleId))]
    public virtual SysConstraintRule? ConstraintRule { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ConstraintRuleId <= 0)
        {
            yield return new ValidationResult("约束规则项的 ConstraintRuleId 必须大于 0。", [nameof(ConstraintRuleId)]);
        }

        if (TargetId <= 0)
        {
            yield return new ValidationResult("约束规则项的 TargetId 必须大于 0。", [nameof(TargetId)]);
        }

        if (ConstraintGroup < 0)
        {
            yield return new ValidationResult("ConstraintGroup 不能为负数。", [nameof(ConstraintGroup)]);
        }
    }
}
