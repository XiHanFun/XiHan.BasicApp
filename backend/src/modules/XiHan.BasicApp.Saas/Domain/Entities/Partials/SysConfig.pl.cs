#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfig.pl
// Guid:1d28152c-d6e9-4396-addb-b479254bad35
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统配置实体扩展
/// </summary>
public partial class SysConfig : IValidatableObject
{
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
        if (IsGlobal && TenantId != 0)
        {
            yield return new ValidationResult("全局配置必须使用 TenantId = 0。", [nameof(IsGlobal), nameof(TenantId)]);
        }

        if (TenantId == 0 && !IsGlobal)
        {
            yield return new ValidationResult("平台租户（TenantId=0）的配置必须标记为全局。", [nameof(TenantId), nameof(IsGlobal)]);
        }

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
