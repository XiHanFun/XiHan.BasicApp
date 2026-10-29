// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Organization;

/// <summary>
/// 组织包的消息键。
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>。
/// </remarks>
public static class OrganizationMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.organization";

    /// <summary>
    /// 操作 <c>departments.create</c> 的说明。
    /// </summary>
    public const string CreateDepartmentDescription = "departments.create.description";

    /// <summary>
    /// 操作 <c>departments.get</c> 的说明。
    /// </summary>
    public const string GetDepartmentDescription = "departments.get.description";

    /// <summary>
    /// 操作 <c>departments.tree</c> 的说明。
    /// </summary>
    public const string DepartmentTreeDescription = "departments.tree.description";

    /// <summary>
    /// 操作 <c>departments.update</c> 的说明。
    /// </summary>
    public const string UpdateDepartmentDescription = "departments.update.description";

    /// <summary>
    /// 操作 <c>departments.move</c> 的说明。
    /// </summary>
    public const string MoveDepartmentDescription = "departments.move.description";

    /// <summary>
    /// 操作 <c>departments.delete</c> 的说明。
    /// </summary>
    public const string DeleteDepartmentsDescription = "departments.delete.description";

    /// <summary>
    /// 操作 <c>positions.create</c> 的说明。
    /// </summary>
    public const string CreatePositionDescription = "positions.create.description";

    /// <summary>
    /// 操作 <c>positions.get</c> 的说明。
    /// </summary>
    public const string GetPositionDescription = "positions.get.description";

    /// <summary>
    /// 操作 <c>positions.list</c> 的说明。
    /// </summary>
    public const string ListPositionsDescription = "positions.list.description";

    /// <summary>
    /// 操作 <c>positions.update</c> 的说明。
    /// </summary>
    public const string UpdatePositionDescription = "positions.update.description";

    /// <summary>
    /// 操作 <c>positions.delete</c> 的说明。
    /// </summary>
    public const string DeletePositionsDescription = "positions.delete.description";

    /// <summary>
    /// 操作 <c>memberships.create</c> 的说明。
    /// </summary>
    public const string CreateMembershipDescription = "memberships.create.description";

    /// <summary>
    /// 操作 <c>memberships.list</c> 的说明。
    /// </summary>
    public const string ListMembershipsDescription = "memberships.list.description";

    /// <summary>
    /// 操作 <c>memberships.update</c> 的说明。
    /// </summary>
    public const string UpdateMembershipDescription = "memberships.update.description";

    /// <summary>
    /// 操作 <c>memberships.delete</c> 的说明。
    /// </summary>
    public const string DeleteMembershipsDescription = "memberships.delete.description";

    /// <summary>
    /// 部门不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string DepartmentNotFound = "organization.department.not_found";

    /// <summary>
    /// 部门编码被占用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string DepartmentCodeTaken = "organization.department.code_taken";

    /// <summary>
    /// 部门还有子部门或成员。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string DepartmentInUse = "organization.department.in_use";

    /// <summary>
    /// 部门不能移到自己或子孙之下。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string CyclicMove = "organization.department.cyclic_move";

    /// <summary>
    /// 岗位不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string PositionNotFound = "organization.position.not_found";

    /// <summary>
    /// 岗位编码被占用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string PositionCodeTaken = "organization.position.code_taken";

    /// <summary>
    /// 岗位还被引用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>、<c>count</c>。
    /// </remarks>
    public const string PositionInUse = "organization.position.in_use";

    /// <summary>
    /// 归属记录不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string MembershipNotFound = "organization.membership.not_found";

    /// <summary>
    /// 用户已在该部门。
    /// </summary>
    /// <remarks>
    /// 参数 <c>userId</c>、<c>departmentId</c>。
    /// </remarks>
    public const string MembershipExists = "organization.membership.exists";

    /// <summary>
    /// 用户不属于当前租户。
    /// </summary>
    /// <remarks>
    /// 参数 <c>userId</c>。
    /// </remarks>
    public const string UserNotInTenant = "organization.membership.user_not_in_tenant";

    /// <summary>
    /// 筛选不支持。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>、<c>operator</c>。
    /// </remarks>
    public const string FilterUnsupported = "organization.query.filter_unsupported";

    /// <summary>
    /// 排序不支持。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string SortUnsupported = "organization.query.sort_unsupported";

    /// <summary>
    /// 组织只在租户侧管理。
    /// </summary>
    public const string TenantOnly = "organization.tenant_only";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(OrganizationMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
