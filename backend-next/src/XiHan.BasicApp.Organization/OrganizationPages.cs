// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Organization;

/// <summary>
/// 组织包的页面登记。
/// </summary>
/// <remarks>
/// 权限只在租户侧，平台主体的菜单树里自然没有。归属在部门页里维护，不单独成页。
/// </remarks>
public static class OrganizationPages
{
    /// <summary>
    /// 部门页（树、详情、成员）。
    /// </summary>
    public static PageDescriptor Departments { get; } = new("organization.departments", "/organization/departments", "organization.departments.title", "lucide:network", 110, OrganizationPermissions.DepartmentsRead);

    /// <summary>
    /// 岗位页。
    /// </summary>
    public static PageDescriptor Positions { get; } = new("organization.positions", "/organization/positions", "organization.positions.title", "lucide:briefcase", 120, OrganizationPermissions.PositionsRead);

    /// <summary>
    /// 全部顶层页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Departments, Positions];
}
