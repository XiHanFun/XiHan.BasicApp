#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserDepartment.Expand
// Guid:3d28152c-d6e9-4396-addb-b479254bad23
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 03:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
