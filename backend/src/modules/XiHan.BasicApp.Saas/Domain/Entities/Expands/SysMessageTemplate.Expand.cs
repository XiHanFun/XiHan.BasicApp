// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统消息模板实体扩展
/// </summary>
public partial class SysMessageTemplate : IValidatableObject
{
    /// <summary>
    /// 是否平台级全局模板（派生属性：TenantId == 0 即作为所有租户的默认模板；不落库）
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
        if (string.IsNullOrWhiteSpace(TemplateCode))
        {
            yield return new ValidationResult("TemplateCode 不能为空。", [nameof(TemplateCode)]);
        }

        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            yield return new ValidationResult("TemplateName 不能为空。", [nameof(TemplateName)]);
        }

        if (string.IsNullOrWhiteSpace(Content))
        {
            yield return new ValidationResult("Content 不能为空。", [nameof(Content)]);
        }
    }
}
