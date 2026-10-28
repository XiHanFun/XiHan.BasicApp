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

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(AccessMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
