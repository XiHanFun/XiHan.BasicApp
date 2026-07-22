// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统配置实体扩展
/// </summary>
public partial class SysConfig : IValidatableObject
{
    /// <summary>
    /// 是否平台级全局配置（派生属性：TenantId == 0 即对所有租户生效；不落库，消除与 TenantId 漂移的风险）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsGlobal => TenantId == 0;

    /// <summary>
    /// 租户信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(TenantId))]
    public virtual SysTenant? Tenant { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ConfigKey))
        {
            yield return new ValidationResult("ConfigKey 不能为空。", [nameof(ConfigKey)]);
        }

        if (string.IsNullOrWhiteSpace(ConfigName))
        {
            yield return new ValidationResult("ConfigName 不能为空。", [nameof(ConfigName)]);
        }
    }
}
