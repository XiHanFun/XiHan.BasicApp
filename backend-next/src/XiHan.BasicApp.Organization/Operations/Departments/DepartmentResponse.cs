// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// 部门的网络契约。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="ParentId">直接上级；为空即顶层。</param>
/// <param name="LeaderUserId">负责人。</param>
/// <param name="Status">状态。</param>
/// <param name="Order">同级排序号。</param>
/// <param name="Description">说明。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
public sealed record DepartmentResponse(
    Guid Id,
    string Code,
    string Name,
    Guid? ParentId,
    Guid? LeaderUserId,
    OrganizationStatus Status,
    int Order,
    string? Description,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static DepartmentResponse From(SysDepartment department)
    {
        ArgumentNullException.ThrowIfNull(department);
        return new DepartmentResponse(department.Id, department.Code, department.Name, department.ParentId, department.LeaderUserId, department.Status, department.Order, department.Description, department.CreatedAt);
    }
}
