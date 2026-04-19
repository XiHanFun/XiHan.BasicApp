#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantEdition.pl
// Guid:a7b8c9d0-e1f2-3456-0123-567890123407
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 租户版本套餐实体扩展
/// </summary>
public partial class SysTenantEdition : IValidatableObject
{
    /// <summary>
    /// 版本可用权限列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysTenantEditionPermission.EditionId))]
    public virtual List<SysTenantEditionPermission>? EditionPermissions { get; set; }

    /// <summary>
    /// 权限列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(typeof(SysTenantEditionPermission), nameof(SysTenantEditionPermission.EditionId), nameof(SysTenantEditionPermission.PermissionId))]
    public virtual List<SysPermission>? Permissions { get; set; }

    /// <summary>
    /// 使用此版本的租户列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysTenant.EditionId))]
    public virtual List<SysTenant>? Tenants { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TenantId != 0)
        {
            yield return new ValidationResult("租户版本套餐是平台级实体，必须使用 TenantId = 0。", [nameof(TenantId)]);
        }

        if (string.IsNullOrWhiteSpace(EditionCode))
        {
            yield return new ValidationResult("EditionCode 不能为空。", [nameof(EditionCode)]);
        }

        if (string.IsNullOrWhiteSpace(EditionName))
        {
            yield return new ValidationResult("EditionName 不能为空。", [nameof(EditionName)]);
        }
    }
}
