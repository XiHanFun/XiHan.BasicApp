// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包的权限码（<c>access.&lt;资源&gt;.&lt;动作&gt;</c>）与它们的描述；在 <see cref="AccessFeature.Configure"/> 登记进内核，并进入权限目录。
/// </summary>
public static class AccessPermissions
{
    /// <summary>
    /// 查看角色（含成员与授予）。
    /// </summary>
    public const string RolesRead = "access.roles.read";

    /// <summary>
    /// 创建角色。
    /// </summary>
    public const string RolesCreate = "access.roles.create";

    /// <summary>
    /// 修改角色。
    /// </summary>
    public const string RolesUpdate = "access.roles.update";

    /// <summary>
    /// 删除角色。
    /// </summary>
    public const string RolesDelete = "access.roles.delete";

    /// <summary>
    /// 给角色授予 / 撤销权限。
    /// </summary>
    public const string RolesGrant = "access.roles.grant";

    /// <summary>
    /// 给角色指派 / 解除成员。
    /// </summary>
    public const string RolesAssign = "access.roles.assign";

    /// <summary>
    /// 查看权限目录。
    /// </summary>
    public const string PermissionsRead = "access.permissions.read";

    /// <summary>
    /// 查看菜单：<c>menus.list / get</c>。
    /// </summary>
    public const string MenusRead = "access.menus.read";

    /// <summary>
    /// 新建目录与外链：<c>menus.create</c>。
    /// </summary>
    public const string MenusCreate = "access.menus.create";

    /// <summary>
    /// 修改菜单与恢复页面默认：<c>menus.update / reset</c>。
    /// </summary>
    public const string MenusUpdate = "access.menus.update";

    /// <summary>
    /// 删除目录与外链：<c>menus.delete</c>。
    /// </summary>
    public const string MenusDelete = "access.menus.delete";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(RolesRead, "Read roles", PermissionScope.Both, description: "View roles, their members and their permissions."),
        new(RolesCreate, "Create roles", PermissionScope.Both, audited: true, description: "Create roles on the current side (platform or tenant)."),
        new(RolesUpdate, "Update roles", PermissionScope.Both, audited: true, description: "Edit role names, descriptions, order and status."),
        new(RolesDelete, "Delete roles", PermissionScope.Both, audited: true, description: "Delete roles together with their members and permissions."),
        new(RolesGrant, "Grant permissions", PermissionScope.Both, audited: true, description: "Grant permissions to roles and revoke them."),
        new(RolesAssign, "Assign members", PermissionScope.Both, audited: true, description: "Assign users to roles and remove them."),
        new(PermissionsRead, "Read permissions", PermissionScope.Both, description: "View the permission catalog of the current side."),
        new(MenusRead, "Read menus", PermissionScope.Platform, description: "View the whole menu tree, including hidden and operator-created nodes."),
        new(MenusCreate, "Create menus", PermissionScope.Platform, audited: true, description: "Create directories and external links in the menu."),
        new(MenusUpdate, "Update menus", PermissionScope.Platform, audited: true, description: "Change how menu nodes are presented and restore page defaults."),
        new(MenusDelete, "Delete menus", PermissionScope.Platform, audited: true, description: "Delete operator-created directories and links."),
    ];
}
