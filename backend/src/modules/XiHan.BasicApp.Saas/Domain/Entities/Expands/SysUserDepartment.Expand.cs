// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户部门关联实体扩展
/// </summary>
public partial class SysUserDepartment : IValidatableObject
{
    /// <summary>
    /// 用户信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public virtual SysUser? User { get; set; }

    /// <summary>
    /// 部门信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(DepartmentId))]
    public virtual SysDepartment? Department { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UserId <= 0)
        {
            yield return new ValidationResult("用户部门关系的 UserId 必须大于 0。", [nameof(UserId)]);
        }

        if (DepartmentId <= 0)
        {
            yield return new ValidationResult("用户部门关系的 DepartmentId 必须大于 0。", [nameof(DepartmentId)]);
        }
    }
}
