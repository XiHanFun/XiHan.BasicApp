// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包的消息键；各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（<c>-</c> 换成 <c>_</c>）。
/// </summary>
public static class AccessMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.access";

    /// <summary>
    /// 操作 <c>roles.create</c> 的说明。
    /// </summary>
    public const string CreateRoleDescription = "roles.create.description";

    /// <summary>
    /// 操作 <c>roles.get</c> 的说明。
    /// </summary>
    public const string GetRoleDescription = "roles.get.description";

    /// <summary>
    /// 操作 <c>roles.list</c> 的说明。
    /// </summary>
    public const string ListRolesDescription = "roles.list.description";

    /// <summary>
    /// 操作 <c>roles.update</c> 的说明。
    /// </summary>
    public const string UpdateRoleDescription = "roles.update.description";

    /// <summary>
    /// 操作 <c>roles.delete</c> 的说明。
    /// </summary>
    public const string DeleteRolesDescription = "roles.delete.description";

    /// <summary>
    /// 操作 <c>roles.grant</c> 的说明。
    /// </summary>
    public const string GrantRoleDescription = "roles.grant.description";

    /// <summary>
    /// 操作 <c>roles.revoke</c> 的说明。
    /// </summary>
    public const string RevokeRoleDescription = "roles.revoke.description";

    /// <summary>
    /// 操作 <c>roles.assign</c> 的说明。
    /// </summary>
    public const string AssignRoleDescription = "roles.assign.description";

    /// <summary>
    /// 操作 <c>roles.unassign</c> 的说明。
    /// </summary>
    public const string UnassignRoleDescription = "roles.unassign.description";

    /// <summary>
    /// 操作 <c>roles.members</c> 的说明。
    /// </summary>
    public const string RoleMembersDescription = "roles.members.description";

    /// <summary>
    /// 操作 <c>permissions.list</c> 的说明。
    /// </summary>
    public const string ListPermissionsDescription = "permissions.list.description";

    /// <summary>
    /// 操作 <c>auth.permissions</c> 的说明。
    /// </summary>
    public const string PermissionsDescription = "auth.permissions.description";

    /// <summary>
    /// 角色不存在；参数 <c>id</c>。
    /// </summary>
    public const string RoleNotFound = "access.role.not_found";

    /// <summary>
    /// 角色码已被占用；参数 <c>code</c>。
    /// </summary>
    public const string RoleCodeTaken = "access.role.code_taken";

    /// <summary>
    /// 内建角色不能删除；参数 <c>code</c>。
    /// </summary>
    public const string BuiltInRole = "access.role.built_in";

    /// <summary>
    /// 列表筛选不受支持；参数 <c>field</c>、<c>operator</c>。
    /// </summary>
    public const string FilterUnsupported = "access.role.filter_unsupported";

    /// <summary>
    /// 列表排序字段不受支持；参数 <c>field</c>。
    /// </summary>
    public const string SortUnsupported = "access.role.sort_unsupported";

    /// <summary>
    /// 权限码不在目录里；参数 <c>code</c>。
    /// </summary>
    public const string PermissionUnknown = "access.permission.unknown";

    /// <summary>
    /// 权限作用侧不符；参数 <c>code</c>、<c>side</c>。
    /// </summary>
    public const string PermissionScopeMismatch = "access.permission.scope_mismatch";

    /// <summary>
    /// 用户不存在或不在角色所属租户；参数 <c>id</c>。
    /// </summary>
    public const string MemberNotFound = "access.member.not_found";

    /// <summary>
    /// 操作 <c>roles.by-user</c> 的说明。
    /// </summary>
    public const string RolesByUserDescription = "roles.by_user.description";

    /// <summary>
    /// 平台角色的数据范围只能是全部；参数 <c>code</c>。
    /// </summary>
    public const string DataScopeNotApplicable = "access.role.data_scope_not_applicable";

    /// <summary>
    /// 自定义数据范围至少一个部门；参数 <c>code</c>。
    /// </summary>
    public const string CustomScopeEmpty = "access.role.custom_scope_empty";

    /// <summary>
    /// 部门不在当前租户；参数 <c>id</c>。
    /// </summary>
    public const string DepartmentNotFound = "access.department.not_found";

    /// <summary>
    /// 租户主体只能看租户侧的权限目录。
    /// </summary>
    public const string PlatformSideDenied = "access.permission.side_denied";

    /// <summary>
    /// 操作 <c>menus.list</c> 的说明。
    /// </summary>
    public const string ListMenusDescription = "menus.list.description";

    /// <summary>
    /// 操作 <c>menus.get</c> 的说明。
    /// </summary>
    public const string GetMenuDescription = "menus.get.description";

    /// <summary>
    /// 操作 <c>menus.create</c> 的说明。
    /// </summary>
    public const string CreateMenuDescription = "menus.create.description";

    /// <summary>
    /// 操作 <c>menus.update</c> 的说明。
    /// </summary>
    public const string UpdateMenuDescription = "menus.update.description";

    /// <summary>
    /// 操作 <c>menus.reset</c> 的说明。
    /// </summary>
    public const string ResetMenuDescription = "menus.reset.description";

    /// <summary>
    /// 操作 <c>menus.delete</c> 的说明。
    /// </summary>
    public const string DeleteMenusDescription = "menus.delete.description";

    /// <summary>
    /// 菜单节点不存在；参数 <c>id</c>。
    /// </summary>
    public const string MenuNotFound = "access.menu.not_found";

    /// <summary>
    /// 菜单编码已被占用；参数 <c>code</c>。
    /// </summary>
    public const string MenuCodeTaken = "access.menu.code_taken";

    /// <summary>
    /// 页面菜单不能删除；参数 <c>code</c>。
    /// </summary>
    public const string MenuBuiltIn = "access.menu.built_in";

    /// <summary>
    /// 目录下还有节点；参数 <c>code</c>。
    /// </summary>
    public const string MenuHasChildren = "access.menu.has_children";

    /// <summary>
    /// 上级节点不合规。
    /// </summary>
    public const string MenuParentInvalid = "access.menu.parent_invalid";

    /// <summary>
    /// 外链地址不合规。
    /// </summary>
    public const string MenuUrlInvalid = "access.menu.url_invalid";

    /// <summary>
    /// 页面菜单的来源字段归页面登记。
    /// </summary>
    public const string MenuPageManaged = "access.menu.page_managed";

    /// <summary>
    /// 目录与外链必须有标题。
    /// </summary>
    public const string MenuTitleRequired = "access.menu.title_required";

    /// <summary>
    /// 不是页面菜单，没有默认值可恢复；参数 <c>code</c>。
    /// </summary>
    public const string MenuNotPage = "access.menu.not_page";

    /// <summary>
    /// 菜单只能在平台侧管理。
    /// </summary>
    public const string MenuPlatformOnly = "access.menu.platform_only";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(AccessMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
