// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统权限实体扩展
/// </summary>
public partial class SysPermission : IValidatableObject
{
    /// <summary>
    /// 是否平台级全局权限（派生属性：TenantId == 0 即所有租户共享，作为平台模板；不落库，消除与 TenantId 漂移的风险）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsGlobal => TenantId == 0;

    /// <summary>
    /// 关联的资源（多权限可关联同一资源，ManyToOne）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(ResourceId))]
    public virtual SysResource? Resource { get; set; }

    /// <summary>
    /// 关联的操作（多权限可关联同一操作，ManyToOne）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(OperationId))]
    public virtual SysOperation? Operation { get; set; }

    /// <summary>
    /// 角色权限关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysRolePermission.PermissionId))]
    public virtual List<SysRolePermission>? RolePermissions { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(typeof(SysRolePermission), nameof(SysRolePermission.PermissionId), nameof(SysRolePermission.RoleId))]
    public virtual List<SysRole>? Roles { get; set; }

    /// <summary>
    /// 用户权限关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysUserPermission.PermissionId))]
    public virtual List<SysUserPermission>? UserPermissions { get; set; }

    /// <summary>
    /// 用户列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(typeof(SysUserPermission), nameof(SysUserPermission.PermissionId), nameof(SysUserPermission.UserId))]
    public virtual List<SysUser>? Users { get; set; }

    /// <summary>
    /// 版本权限映射列表（此权限被哪些版本包含）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysTenantEditionPermission.PermissionId))]
    public virtual List<SysTenantEditionPermission>? EditionPermissions { get; set; }

    /// <summary>
    /// 关联菜单列表（哪些菜单依赖此权限做可见性控制）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysMenu.PermissionId))]
    public virtual List<SysMenu>? Menus { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ResourceId <= 0)
        {
            yield return new ValidationResult("Permission 的 ResourceId 必须大于 0。", [nameof(ResourceId)]);
        }

        if (OperationId <= 0)
        {
            yield return new ValidationResult("Permission 的 OperationId 必须大于 0。", [nameof(OperationId)]);
        }

        if (string.IsNullOrWhiteSpace(PermissionCode))
        {
            yield return new ValidationResult("PermissionCode 不能为空。", [nameof(PermissionCode)]);
        }

        if (string.IsNullOrWhiteSpace(PermissionName))
        {
            yield return new ValidationResult("PermissionName 不能为空。", [nameof(PermissionName)]);
        }
    }
}
