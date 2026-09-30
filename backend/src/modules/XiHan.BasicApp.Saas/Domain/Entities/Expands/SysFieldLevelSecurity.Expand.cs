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
    /// 是否平台规则（派生属性：TenantId == 0 即对所有租户生效，只能在平台维护；不落库）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsGlobal => TenantId == 0;

    /// <summary>
    /// 部分脱敏参数上限：保留位数超过它就谈不上脱敏
    /// </summary>
    public const int MaxMaskKeep = 32;

    /// <summary>
    /// 固定文本最大长度（与列宽一致）
    /// </summary>
    public const int MaxMaskReplacementLength = 100;

    /// <summary>
    /// 校验实体自身的业务规则（与领域服务的写入校验一致，存量数据落库前的最后一道）
    /// </summary>
    /// <param name="validationContext">校验上下文</param>
    /// <returns>校验失败项集合，全部通过时为空集合</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TargetId <= 0)
        {
            yield return new ValidationResult("字段级安全规则的 TargetId 必须大于 0。", [nameof(TargetId)]);
        }

        if (string.IsNullOrWhiteSpace(EntityName))
        {
            yield return new ValidationResult("字段级安全规则的 EntityName 不能为空。", [nameof(EntityName)]);
        }

        if (string.IsNullOrWhiteSpace(FieldName))
        {
            yield return new ValidationResult("字段级安全规则的 FieldName 不能为空。", [nameof(FieldName)]);
        }

        if (MaskStrategy == FieldMaskStrategy.None && IsEditable)
        {
            yield return new ValidationResult("明文且可编辑的规则什么都没限制，至少要脱敏或只读。", [nameof(MaskStrategy), nameof(IsEditable)]);
        }

        var isPartial = MaskStrategy == FieldMaskStrategy.PartialMask;
        if (isPartial != (MaskKeepHead.HasValue && MaskKeepTail.HasValue))
        {
            yield return new ValidationResult("保留前几位、后几位只在部分脱敏时填写，且部分脱敏必须填写。", [nameof(MaskKeepHead), nameof(MaskKeepTail)]);
        }

        if ((MaskStrategy == FieldMaskStrategy.Redact) != !string.IsNullOrWhiteSpace(MaskReplacement))
        {
            yield return new ValidationResult("固定文本只在固定文本方式下填写，且该方式必须填写。", [nameof(MaskReplacement)]);
        }
    }
}
