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
    /// 设置角色的上级（继承）。
    /// </summary>
    public const string RolesSetParents = "access.roles.set-parents";

    /// <summary>
    /// 查看用户的直接授权：<c>user-grants.list</c>。
    /// </summary>
    public const string UserGrantsRead = "access.user-grants.read";

    /// <summary>
    /// 给用户直接授予、拒绝与撤销：<c>user-grants.grant / deny / revoke</c>。
    /// </summary>
    public const string UserGrantsGrant = "access.user-grants.grant";

    /// <summary>
    /// 查看用户级数据范围覆盖：<c>user-data-scopes.get</c>（只在租户侧）。
    /// </summary>
    public const string UserDataScopesRead = "access.user-data-scopes.read";

    /// <summary>
    /// 设置或清除用户级数据范围覆盖：<c>user-data-scopes.set</c>（只在租户侧）。
    /// </summary>
    public const string UserDataScopesUpdate = "access.user-data-scopes.update";

    /// <summary>
    /// 查看字段安全规则与实体目录：<c>field-security.list / get / entities</c>。
    /// </summary>
    public const string FieldSecurityRead = "access.field-security.read";

    /// <summary>
    /// 新建字段安全规则。
    /// </summary>
    public const string FieldSecurityCreate = "access.field-security.create";

    /// <summary>
    /// 修改与启停字段安全规则。
    /// </summary>
    public const string FieldSecurityUpdate = "access.field-security.update";

    /// <summary>
    /// 删除字段安全规则。
    /// </summary>
    public const string FieldSecurityDelete = "access.field-security.delete";

    /// <summary>
    /// 查看委托。
    /// </summary>
    public const string DelegationsRead = "access.delegations.read";

    /// <summary>
    /// 新建委托。
    /// </summary>
    public const string DelegationsCreate = "access.delegations.create";

    /// <summary>
    /// 撤销委托。
    /// </summary>
    public const string DelegationsRevoke = "access.delegations.revoke";

    /// <summary>
    /// 删除委托记录。
    /// </summary>
    public const string DelegationsDelete = "access.delegations.delete";

    /// <summary>
    /// 查看权限条件与条件属性目录。
    /// </summary>
    public const string PermissionConditionsRead = "access.permission-conditions.read";

    /// <summary>
    /// 新建权限条件。
    /// </summary>
    public const string PermissionConditionsCreate = "access.permission-conditions.create";

    /// <summary>
    /// 修改与启停权限条件。
    /// </summary>
    public const string PermissionConditionsUpdate = "access.permission-conditions.update";

    /// <summary>
    /// 删除权限条件。
    /// </summary>
    public const string PermissionConditionsDelete = "access.permission-conditions.delete";

    /// <summary>
    /// 条件试算、影响试算与盘点。
    /// </summary>
    public const string PermissionConditionsTrial = "access.permission-conditions.trial";

    /// <summary>
    /// 查看访问策略与模板：<c>policies.list / get / templates</c>。
    /// </summary>
    public const string PoliciesRead = "access.policies.read";

    /// <summary>
    /// 新建访问策略（一律只记录）：<c>policies.create</c>。
    /// </summary>
    public const string PoliciesCreate = "access.policies.create";

    /// <summary>
    /// 修改访问策略、改动作与启停：<c>policies.update / set-action / set-status</c>。
    /// </summary>
    public const string PoliciesUpdate = "access.policies.update";

    /// <summary>
    /// 删除访问策略：<c>policies.delete</c>。
    /// </summary>
    public const string PoliciesDelete = "access.policies.delete";

    /// <summary>
    /// 试运行访问策略：<c>policies.trial</c>。
    /// </summary>
    public const string PoliciesTrial = "access.policies.trial";

    /// <summary>
    /// 查看登录失败自动封禁的参数与当前封禁（只在平台）：<c>login-bans.list</c>。
    /// </summary>
    public const string LoginBansRead = "access.login-bans.read";

    /// <summary>
    /// 解封一个地址（只在平台）：<c>login-bans.lift</c>。
    /// </summary>
    public const string LoginBansLift = "access.login-bans.lift";

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
        new(RolesSetParents, "Set role parents", PermissionScope.Both, audited: true, description: "Make roles inherit the permissions of other roles on the same side, and remove that inheritance."),
        new(UserGrantsRead, "Read user grants", PermissionScope.Both, description: "View the permissions granted or denied directly to users."),
        new(UserGrantsGrant, "Manage user grants", PermissionScope.Both, audited: true, description: "Grant, deny and revoke permissions directly on users, with optional validity windows."),
        new(UserDataScopesRead, "Read user data scopes", PermissionScope.Tenant, description: "View per-user data scope overrides."),
        new(UserDataScopesUpdate, "Update user data scopes", PermissionScope.Tenant, audited: true, description: "Override a user's data scope in place of their roles' scopes, or clear the override."),
        new(FieldSecurityRead, "Read field security", PermissionScope.Both, description: "View field security rules and the entities and fields they can protect."),
        new(FieldSecurityCreate, "Create field security", PermissionScope.Both, audited: true, description: "Create rules that mask or lock fields for roles, users or departments."),
        new(FieldSecurityUpdate, "Update field security", PermissionScope.Both, audited: true, description: "Edit, enable and disable field security rules."),
        new(FieldSecurityDelete, "Delete field security", PermissionScope.Both, audited: true, description: "Delete field security rules."),
        new(DelegationsRead, "Read delegations", PermissionScope.Both, description: "View time-limited delegations of permissions and roles between users."),
        new(DelegationsCreate, "Create delegations", PermissionScope.Both, audited: true, description: "Let a user hand one of their permissions or roles to another user for a limited time."),
        new(DelegationsRevoke, "Revoke delegations", PermissionScope.Both, audited: true, description: "End a pending or active delegation before it expires."),
        new(DelegationsDelete, "Delete delegations", PermissionScope.Both, audited: true, description: "Delete delegation records; an active one stops taking effect."),
        new(PermissionConditionsRead, "Read permission conditions", PermissionScope.Both, description: "View the conditions on role and user grants and the condition attribute catalog."),
        new(PermissionConditionsCreate, "Create permission conditions", PermissionScope.Both, audited: true, description: "Add conditions to a role or user grant or denial: met to grant, or met to deny."),
        new(PermissionConditionsUpdate, "Update permission conditions", PermissionScope.Both, audited: true, description: "Edit, enable and disable permission conditions."),
        new(PermissionConditionsDelete, "Delete permission conditions", PermissionScope.Both, audited: true, description: "Delete permission conditions."),
        new(PermissionConditionsTrial, "Trial permission conditions", PermissionScope.Both, description: "Try how conditions decide for a user and a request, list who a grant affects, and inventory the conditions of the current side."),
        new(PoliciesRead, "Read access policies", PermissionScope.Both, description: "View the access policies of the current side and the preset templates."),
        new(PoliciesCreate, "Create access policies", PermissionScope.Both, audited: true, description: "Create access policies; a new policy only logs its hits until its action is changed."),
        new(PoliciesUpdate, "Update access policies", PermissionScope.Both, audited: true, description: "Edit access policies, switch them between log only, deny and allow, and enable or disable them."),
        new(PoliciesDelete, "Delete access policies", PermissionScope.Both, audited: true, description: "Delete access policies."),
        new(PoliciesTrial, "Trial access policies", PermissionScope.Both, description: "Try how a saved or draft access policy decides for a given request."),
        new(LoginBansRead, "Read login bans", PermissionScope.Platform, description: "View the login auto-ban settings and the addresses banned for too many failed sign-ins."),
        new(LoginBansLift, "Lift login bans", PermissionScope.Platform, audited: true, description: "Lift the ban on an address before it expires, together with its current failure count."),
        new(PermissionsRead, "Read permissions", PermissionScope.Both, description: "View the permission catalog of the current side."),
        new(MenusRead, "Read menus", PermissionScope.Platform, description: "View the whole menu tree, including hidden and operator-created nodes."),
        new(MenusCreate, "Create menus", PermissionScope.Platform, audited: true, description: "Create directories and external links in the menu."),
        new(MenusUpdate, "Update menus", PermissionScope.Platform, audited: true, description: "Change how menu nodes are presented and restore page defaults."),
        new(MenusDelete, "Delete menus", PermissionScope.Platform, audited: true, description: "Delete operator-created directories and links."),
    ];
}
