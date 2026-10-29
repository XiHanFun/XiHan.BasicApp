// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Auditing;

/// <summary>
/// 权限变更日志的变更类型。
/// </summary>
public enum PermissionChangeType
{
    /// <summary>
    /// 给角色授予权限。
    /// </summary>
    RoleGrant = 0,

    /// <summary>
    /// 撤销角色的权限。
    /// </summary>
    RoleRevoke = 1,

    /// <summary>
    /// 把用户加入角色。
    /// </summary>
    RoleAssign = 2,

    /// <summary>
    /// 把用户移出角色。
    /// </summary>
    RoleUnassign = 3,

    /// <summary>
    /// 改角色的数据范围。
    /// </summary>
    RoleDataScope = 4,

    /// <summary>
    /// 删除角色（连同成员与授予）。
    /// </summary>
    RoleDelete = 5,

    /// <summary>
    /// 改套餐的权限白名单。
    /// </summary>
    EditionPermissions = 6,

    /// <summary>
    /// 给租户换套餐。
    /// </summary>
    TenantEdition = 7,

    /// <summary>
    /// 角色拒绝权限（Deny）。
    /// </summary>
    RoleDeny = 8,

    /// <summary>
    /// 改成员指派的有效期。
    /// </summary>
    RoleAssignmentWindow = 9,

    /// <summary>
    /// 给用户直接授予权限。
    /// </summary>
    UserGrant = 10,

    /// <summary>
    /// 给用户直接拒绝权限（Deny）。
    /// </summary>
    UserDeny = 11,

    /// <summary>
    /// 撤销用户的直接授予或拒绝。
    /// </summary>
    UserRevoke = 12,

    /// <summary>
    /// 设置或清除用户级数据范围覆盖。
    /// </summary>
    UserDataScope = 13,

    /// <summary>
    /// 新建、修改或删除字段安全规则。
    /// </summary>
    FieldSecurity = 14,

    /// <summary>
    /// 设置角色的上级（继承）。
    /// </summary>
    RoleInheritance = 15,

    /// <summary>
    /// 新建、撤销或删除委托。
    /// </summary>
    Delegation = 16,

    /// <summary>
    /// 新建、修改、启停或删除权限条件。
    /// </summary>
    ConditionChange = 17,
}
