// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 业务编号规则实体扩展
/// </summary>
public partial class SysNumberingRule : IValidatableObject
{
    /// <summary>
    /// 是否平台级全局规则（派生属性：TenantId == 0 即所有租户共享同一条流水序列；不落库，消除与 TenantId 漂移的风险）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsGlobal => TenantId == 0;

    /// <summary>
    /// 校验实体自身的业务规则
    /// </summary>
    /// <param name="validationContext">校验上下文</param>
    /// <returns>校验失败项集合，全部通过时为空集合</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(RuleCode))
        {
            yield return new ValidationResult("规则编码不能为空。", [nameof(RuleCode)]);
        }

        if (string.IsNullOrWhiteSpace(RuleName))
        {
            yield return new ValidationResult("规则名称不能为空。", [nameof(RuleName)]);
        }

        if (SerialLength is < 1 or > 18)
        {
            yield return new ValidationResult("流水位数必须在 1 至 18 之间。", [nameof(SerialLength)]);
        }

        if (string.IsNullOrWhiteSpace(TimeZoneId))
        {
            yield return new ValidationResult("规则时区不能为空。", [nameof(TimeZoneId)]);
        }

        if (CurrentPeriod is null)
        {
            yield return new ValidationResult("当前周期不能为 null，未发号时应为空字符串。", [nameof(CurrentPeriod)]);
        }
    }
}
