// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 租户版本可用权限映射实体扩展
/// </summary>
public partial class SysTenantEditionPermission : IValidatableObject
{
    /// <summary>
    /// 版本信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(EditionId))]
    public virtual SysTenantEdition? Edition { get; set; }

    /// <summary>
    /// 权限信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(PermissionId))]
    public virtual SysPermission? Permission { get; set; }

    /// <summary>
    /// 校验实体自身的业务规则
    /// </summary>
    /// <param name="validationContext">校验上下文</param>
    /// <returns>校验失败项集合，全部通过时为空集合</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TenantId != 0)
        {
            yield return new ValidationResult("版本权限映射是平台级实体，必须使用 TenantId = 0。", [nameof(TenantId)]);
        }

        if (EditionId <= 0)
        {
            yield return new ValidationResult("EditionId 必须大于 0。", [nameof(EditionId)]);
        }

        if (PermissionId <= 0)
        {
            yield return new ValidationResult("PermissionId 必须大于 0。", [nameof(PermissionId)]);
        }
    }
}
