#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConstraintRule.pl
// Guid:12345678-9012-3456-7890-901234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:48:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统约束规则实体扩展
/// </summary>
public partial class SysConstraintRule : IValidatableObject
{
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

        if (IsGlobal && TenantId != 0)
        {
            yield return new ValidationResult("全局约束规则必须使用 TenantId = 0。", [nameof(IsGlobal), nameof(TenantId)]);
        }

        if (TenantId == 0 && !IsGlobal)
        {
            yield return new ValidationResult("平台租户（TenantId=0）的约束规则必须标记为全局。", [nameof(TenantId), nameof(IsGlobal)]);
        }

        if (EffectiveTime.HasValue && ExpirationTime.HasValue && EffectiveTime.Value >= ExpirationTime.Value)
        {
            yield return new ValidationResult("生效时间必须早于失效时间。", [nameof(EffectiveTime), nameof(ExpirationTime)]);
        }
    }
}
