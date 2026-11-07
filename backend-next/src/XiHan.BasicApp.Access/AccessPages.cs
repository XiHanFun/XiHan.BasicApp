// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包的页面登记：汇总进菜单种子与 <c>auth.permissions</c> 的菜单树
/// </summary>
public static class AccessPages
{
    /// <summary>
    /// 角色页
    /// </summary>
    public static PageDescriptor Roles { get; } = new("access.roles", "/access/roles", "access.roles.title", "lucide:shield", 200, AccessPermissions.RolesRead, directory: MenuDirectories.SystemPermission);

    /// <summary>
    /// 菜单页（平台侧）
    /// </summary>
    public static PageDescriptor Menus { get; } = new("access.menus", "/access/menus", "access.menus.title", "lucide:menu", 210, AccessPermissions.MenusRead, directory: MenuDirectories.SystemPermission);

    /// <summary>
    /// 权限目录（只读）：各包登记的权限码按包分组
    /// </summary>
    public static PageDescriptor Permissions { get; } = new("access.permissions", "/access/permissions", "access.permissions.title", "lucide:key-round", 205, AccessPermissions.PermissionsRead, directory: MenuDirectories.SystemPermission);

    /// <summary>
    /// 字段安全：按角色、用户或部门给实体字段打码或锁定
    /// </summary>
    public static PageDescriptor FieldSecurity { get; } = new("access.field-security", "/access/field-security", "access.field_security.title", "lucide:eye-off", 209, AccessPermissions.FieldSecurityRead, directory: MenuDirectories.SystemPermission);

    /// <summary>
    /// 委托页：限时把权限或角色交给别的用户
    /// </summary>
    public static PageDescriptor Delegations { get; } = new("access.delegations", "/access/delegations", "access.delegations.title", "lucide:file-key", 206, AccessPermissions.PermissionRequestsRead, directory: MenuDirectories.SystemPermission);

    /// <summary>
    /// 访问策略页（Access 包）
    /// </summary>
    public static PageDescriptor Policies { get; } = new("access.policies", "/access/policies", "access.policies.title", "lucide:shield-alert", 207, AccessPermissions.PoliciesRead, directory: MenuDirectories.SystemPermission);

    /// <summary>
    /// 约束规则页（Access 包）
    /// </summary>
    public static PageDescriptor ConstraintRules { get; } = new("access.constraint-rules", "/access/constraint-rules", "access.constraint_rules.title", "lucide:split", 208, AccessPermissions.ConstraintRulesRead, directory: MenuDirectories.SystemPermission);

    /// <summary>
    /// 全部页面
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Roles, Permissions, Delegations, FieldSecurity, Policies, ConstraintRules, Menus];

    /// <summary>
    /// 关于页的包段（页面码的首段）
    /// </summary>
    /// <remarks>
    /// 关于页是壳层自带的页面，不归任何能力包：视图在 <c>packages/views/_core/about</c>，由 Access 代为登记进菜单
    /// </remarks>
    public const string AboutPackage = "about";

    /// <summary>
    /// 关于项目（登录即可见，缺省挂在帮助中心目录下）
    /// </summary>
    public static PageDescriptor AboutProject { get; } = new("about.project", "/about/project", "menu.about_project", "lucide:file-text", 1110, directory: MenuDirectories.About);

    /// <summary>
    /// 关于页的全部页面
    /// </summary>
    public static IReadOnlyList<PageDescriptor> About { get; } = [AboutProject];
}
