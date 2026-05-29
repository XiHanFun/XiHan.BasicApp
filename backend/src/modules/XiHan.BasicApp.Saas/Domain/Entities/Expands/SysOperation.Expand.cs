#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperation.Expand
// Guid:89012345-6789-0123-4567-678901234567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:42:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统操作实体扩展
/// </summary>
public partial class SysOperation : IValidatableObject
{
    /// <summary>
    /// 是否平台级全局操作（派生属性：TenantId == 0 即所有租户共享；不落库，消除与 TenantId 漂移的风险）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsGlobal => TenantId == 0;

    /// <summary>
    /// 操作权限列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysPermission.OperationId))]
    public virtual List<SysPermission>? Permissions { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(OperationCode))
        {
            yield return new ValidationResult("OperationCode 不能为空。", [nameof(OperationCode)]);
        }

        if (string.IsNullOrWhiteSpace(OperationName))
        {
            yield return new ValidationResult("OperationName 不能为空。", [nameof(OperationName)]);
        }
    }
}
