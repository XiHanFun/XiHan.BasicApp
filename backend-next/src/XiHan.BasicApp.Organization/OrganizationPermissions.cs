// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Organization;

/// <summary>
/// 组织包的权限码：全部只在租户侧（部门、岗位、归属都是租户库实体，平台没有组织）。
/// </summary>
public static class OrganizationPermissions
{
    /// <summary>
    /// 查看部门树与部门详情。
    /// </summary>
    public const string DepartmentsRead = "organization.departments.read";

    /// <summary>
    /// 创建部门。
    /// </summary>
    public const string DepartmentsCreate = "organization.departments.create";

    /// <summary>
    /// 修改与移动部门。
    /// </summary>
    public const string DepartmentsUpdate = "organization.departments.update";

    /// <summary>
    /// 删除部门。
    /// </summary>
    public const string DepartmentsDelete = "organization.departments.delete";

    /// <summary>
    /// 查看岗位。
    /// </summary>
    public const string PositionsRead = "organization.positions.read";

    /// <summary>
    /// 创建岗位。
    /// </summary>
    public const string PositionsCreate = "organization.positions.create";

    /// <summary>
    /// 修改岗位。
    /// </summary>
    public const string PositionsUpdate = "organization.positions.update";

    /// <summary>
    /// 删除岗位。
    /// </summary>
    public const string PositionsDelete = "organization.positions.delete";

    /// <summary>
    /// 查看部门成员归属。
    /// </summary>
    public const string MembershipsRead = "organization.memberships.read";

    /// <summary>
    /// 把用户加进部门。
    /// </summary>
    public const string MembershipsCreate = "organization.memberships.create";

    /// <summary>
    /// 修改归属（岗位、主部门、工号、职级、入职时间）。
    /// </summary>
    public const string MembershipsUpdate = "organization.memberships.update";

    /// <summary>
    /// 把用户移出部门。
    /// </summary>
    public const string MembershipsDelete = "organization.memberships.delete";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(DepartmentsRead, "Read departments", PermissionScope.Tenant, description: "View the department tree and department details."),
        new(DepartmentsCreate, "Create departments", PermissionScope.Tenant, audited: true, description: "Create departments anywhere in the tree."),
        new(DepartmentsUpdate, "Update departments", PermissionScope.Tenant, audited: true, description: "Edit, disable and move departments."),
        new(DepartmentsDelete, "Delete departments", PermissionScope.Tenant, audited: true, description: "Delete departments that have no children and no members."),
        new(PositionsRead, "Read positions", PermissionScope.Tenant, description: "View positions."),
        new(PositionsCreate, "Create positions", PermissionScope.Tenant, audited: true, description: "Create positions."),
        new(PositionsUpdate, "Update positions", PermissionScope.Tenant, audited: true, description: "Edit and disable positions."),
        new(PositionsDelete, "Delete positions", PermissionScope.Tenant, audited: true, description: "Delete positions that no membership references."),
        new(MembershipsRead, "Read memberships", PermissionScope.Tenant, description: "View which users belong to which departments."),
        new(MembershipsCreate, "Create memberships", PermissionScope.Tenant, audited: true, description: "Add users to departments."),
        new(MembershipsUpdate, "Update memberships", PermissionScope.Tenant, audited: true, description: "Change a membership's position, primary flag, employee number, grade and hire date."),
        new(MembershipsDelete, "Delete memberships", PermissionScope.Tenant, audited: true, description: "Remove users from departments."),
    ];
}
