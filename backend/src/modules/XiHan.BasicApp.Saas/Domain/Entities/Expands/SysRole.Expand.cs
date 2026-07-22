// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统角色实体扩展
/// </summary>
public partial class SysRole : IValidatableObject
{
    /// <summary>
    /// 是否平台级全局角色（派生属性：TenantId == 0 即为平台全局，作为所有租户的模板；不落库，消除与 TenantId 漂移的风险）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsGlobal => TenantId == 0;

    /// <summary>
    /// 用户角色关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysUserRole.RoleId))]
    public virtual List<SysUserRole>? UserRoles { get; set; }

    /// <summary>
    /// 角色权限关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysRolePermission.RoleId))]
    public virtual List<SysRolePermission>? RolePermissions { get; set; }

    /// <summary>
    /// 用户列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(typeof(SysUserRole), nameof(SysUserRole.RoleId), nameof(SysUserRole.UserId))]
    public virtual List<SysUser>? Users { get; set; }

    /// <summary>
    /// 权限列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(typeof(SysRolePermission), nameof(SysRolePermission.RoleId), nameof(SysRolePermission.PermissionId))]
    public virtual List<SysPermission>? Permissions { get; set; }

    /// <summary>
    /// 角色自定义数据权限范围列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysRoleDataScope.RoleId))]
    public virtual List<SysRoleDataScope>? DataScopes { get; set; }

    /// <summary>
    /// 作为祖先角色的层级关系列表（此角色被哪些角色继承）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysRoleHierarchy.AncestorId))]
    public virtual List<SysRoleHierarchy>? AncestorHierarchies { get; set; }

    /// <summary>
    /// 作为后代角色的层级关系列表（此角色继承了哪些角色）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysRoleHierarchy.DescendantId))]
    public virtual List<SysRoleHierarchy>? DescendantHierarchies { get; set; }

    /// <summary>
    /// 会话角色映射列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysSessionRole.RoleId))]
    public virtual List<SysSessionRole>? SessionRoles { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(RoleCode))
        {
            yield return new ValidationResult("RoleCode 不能为空。", [nameof(RoleCode)]);
        }

        if (string.IsNullOrWhiteSpace(RoleName))
        {
            yield return new ValidationResult("RoleName 不能为空。", [nameof(RoleName)]);
        }

        if (RoleType == RoleType.System && !IsGlobal)
        {
            yield return new ValidationResult("系统内置角色必须为全局角色。", [nameof(RoleType), nameof(IsGlobal)]);
        }

        if (RoleType == RoleType.Custom && IsGlobal)
        {
            yield return new ValidationResult("租户自定义角色不能标记为全局角色。", [nameof(RoleType), nameof(IsGlobal)]);
        }

        if (DataScope == DataPermissionScope.All
            && !(IsGlobal && RoleType == RoleType.System))
        {
            yield return new ValidationResult("全部数据权限范围（DataScope=All）仅限平台超管角色（IsGlobal=true 且 RoleType=System）。",
                [nameof(DataScope), nameof(IsGlobal), nameof(RoleType)]);
        }

        if (MaxMembers < 0)
        {
            yield return new ValidationResult("MaxMembers 不能为负数。", [nameof(MaxMembers)]);
        }
    }
}
