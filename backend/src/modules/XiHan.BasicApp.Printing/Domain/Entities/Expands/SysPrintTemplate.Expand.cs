// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Printing.Domain.Entities;

/// <summary>
/// 打印模板实体派生属性与基础数据注解校验。
/// </summary>
public partial class SysPrintTemplate : IValidatableObject
{
    /// <summary>
    /// 是否为平台全局模板；该值由租户标识派生，不单独落库以避免状态漂移。
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsGlobal => TenantId == 0;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(TemplateCode))
        {
            yield return new ValidationResult("模板编码不能为空。", [nameof(TemplateCode)]);
        }

        if (DataSourceCode is not null && string.IsNullOrWhiteSpace(DataSourceCode))
        {
            yield return new ValidationResult("数据源编码为空白时应保存为 null。", [nameof(DataSourceCode)]);
        }

        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            yield return new ValidationResult("模板名称不能为空。", [nameof(TemplateName)]);
        }

        if (string.IsNullOrWhiteSpace(TemplateJson))
        {
            yield return new ValidationResult("打印模板 JSON 不能为空。", [nameof(TemplateJson)]);
        }

        if (string.IsNullOrWhiteSpace(EngineVersion))
        {
            yield return new ValidationResult("打印引擎版本不能为空。", [nameof(EngineVersion)]);
        }
    }
}
