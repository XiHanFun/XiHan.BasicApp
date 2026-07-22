// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统约束规则实体扩展
/// </summary>
public partial class SysConstraintRule : IValidatableObject
{
    /// <summary>
    /// 是否平台级全局约束规则（派生属性：TenantId == 0 即对所有租户生效；不落库，消除与 TenantId 漂移的风险）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsGlobal => TenantId == 0;

    /// <summary>
    /// 约束规则目标项列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysConstraintRuleItem.ConstraintRuleId))]
    public virtual List<SysConstraintRuleItem>? Items { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(RuleCode))
        {
            yield return new ValidationResult("RuleCode 不能为空。", [nameof(RuleCode)]);
        }

        if (string.IsNullOrWhiteSpace(RuleName))
        {
            yield return new ValidationResult("RuleName 不能为空。", [nameof(RuleName)]);
        }

        if (EffectiveTime.HasValue && ExpirationTime.HasValue && EffectiveTime.Value >= ExpirationTime.Value)
        {
            yield return new ValidationResult("生效时间必须早于失效时间。", [nameof(EffectiveTime), nameof(ExpirationTime)]);
        }
    }
}
