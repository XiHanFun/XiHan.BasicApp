// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统资源实体扩展
/// </summary>
public partial class SysResource : IValidatableObject
{
    /// <summary>
    /// 是否平台级全局资源（派生属性：TenantId == 0 即所有租户共享；不落库，消除与 TenantId 漂移的风险）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsGlobal => TenantId == 0;

    /// <summary>
    /// 资源权限列表（一个资源可对应多个权限）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysPermission.ResourceId))]
    public virtual List<SysPermission>? Permissions { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ResourceCode))
        {
            yield return new ValidationResult("ResourceCode 不能为空。", [nameof(ResourceCode)]);
        }

        if (string.IsNullOrWhiteSpace(ResourceName))
        {
            yield return new ValidationResult("ResourceName 不能为空。", [nameof(ResourceName)]);
        }
    }
}
