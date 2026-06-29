#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPosition.Expand
// Guid:3a6c9d2b-8e45-4f1a-ad72-2b3c4d5e6f71
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统岗位实体扩展
/// </summary>
public partial class SysPosition : IValidatableObject
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
        if (string.IsNullOrWhiteSpace(PositionCode))
        {
            yield return new ValidationResult("PositionCode 不能为空。", [nameof(PositionCode)]);
        }

        if (string.IsNullOrWhiteSpace(PositionName))
        {
            yield return new ValidationResult("PositionName 不能为空。", [nameof(PositionName)]);
        }
    }
}
