// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统字段级安全实体扩展
/// </summary>
public partial class SysFieldLevelSecurity : IValidatableObject
{
    /// <summary>
    /// 关联资源
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(ResourceId))]
    public virtual SysResource? Resource { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TargetId <= 0)
        {
            yield return new ValidationResult("字段级安全策略的 TargetId 必须大于 0。", [nameof(TargetId)]);
        }

        if (ResourceId <= 0)
        {
            yield return new ValidationResult("字段级安全策略的 ResourceId 必须大于 0。", [nameof(ResourceId)]);
        }

        if (string.IsNullOrWhiteSpace(FieldName))
        {
            yield return new ValidationResult("字段级安全策略的 FieldName 不能为空。", [nameof(FieldName)]);
        }

        if (Priority < 0)
        {
            yield return new ValidationResult("字段级安全策略的 Priority 不能为负数。", [nameof(Priority)]);
        }

        if (!IsReadable && MaskStrategy == FieldMaskStrategy.None)
        {
            yield return new ValidationResult(
                "不可读字段应配置脱敏策略（MaskStrategy 不应为 None）。",
                [nameof(IsReadable), nameof(MaskStrategy)]);
        }
    }
}
