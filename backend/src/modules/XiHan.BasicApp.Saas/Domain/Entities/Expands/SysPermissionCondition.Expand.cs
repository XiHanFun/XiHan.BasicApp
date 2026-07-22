// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限 ABAC 条件实体扩展
/// </summary>
public partial class SysPermissionCondition : IValidatableObject
{
    /// <summary>
    /// 关联的角色权限
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(RolePermissionId))]
    public virtual SysRolePermission? RolePermission { get; set; }

    /// <summary>
    /// 关联的用户直授权限
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(UserPermissionId))]
    public virtual SysUserPermission? UserPermission { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasRoleBinding = RolePermissionId.HasValue;
        var hasUserBinding = UserPermissionId.HasValue;

        if (hasRoleBinding == hasUserBinding)
        {
            yield return new ValidationResult(
                "ABAC 条件必须且只能绑定到角色权限或用户直授权限中的一种。",
                [nameof(RolePermissionId), nameof(UserPermissionId)]);
        }

        if (ConditionGroup < 0)
        {
            yield return new ValidationResult("ConditionGroup 不能为负数。", [nameof(ConditionGroup)]);
        }

        if (string.IsNullOrWhiteSpace(AttributeName))
        {
            yield return new ValidationResult("AttributeName 不能为空。", [nameof(AttributeName)]);
        }
        else
        {
            var normalizedAttribute = AttributeName.Trim();
            var hasKnownPrefix =
                normalizedAttribute.StartsWith("subject.", StringComparison.OrdinalIgnoreCase) ||
                normalizedAttribute.StartsWith("resource.", StringComparison.OrdinalIgnoreCase) ||
                normalizedAttribute.StartsWith("environment.", StringComparison.OrdinalIgnoreCase);

            if (!hasKnownPrefix)
            {
                yield return new ValidationResult(
                    "AttributeName 必须使用 subject./resource./environment. 命名空间前缀。",
                    [nameof(AttributeName)]);
            }
        }

        if (string.IsNullOrWhiteSpace(ConditionValue))
        {
            yield return new ValidationResult("ConditionValue 不能为空。", [nameof(ConditionValue)]);
        }
    }
}
