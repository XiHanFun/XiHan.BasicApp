// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// 部门的网络契约
/// </summary>
/// <param name="Id">主键</param>
/// <param name="Code">编码</param>
/// <param name="Name">名称</param>
/// <param name="DepartmentType">部门类型</param>
/// <param name="ParentId">直接上级；为空即顶层</param>
/// <param name="LeaderUserId">负责人</param>
/// <param name="Status">状态</param>
/// <param name="Order">同级排序号</param>
/// <param name="Phone">联系电话</param>
/// <param name="Email">邮箱</param>
/// <param name="Address">地址</param>
/// <param name="Description">说明</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
public sealed record DepartmentResponse(
    Guid Id,
    string Code,
    string Name,
    DepartmentType DepartmentType,
    Guid? ParentId,
    Guid? LeaderUserId,
    OrganizationStatus Status,
    int Order,
    string? Phone,
    string? Email,
    string? Address,
    string? Description,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    public static DepartmentResponse From(SysDepartment department)
    {
        ArgumentNullException.ThrowIfNull(department);
        return new DepartmentResponse(department.Id, department.Code, department.Name, department.DepartmentType, department.ParentId, department.LeaderUserId, department.Status, department.Order, department.Phone, department.Email, department.Address, department.Description, department.CreatedAt);
    }
}
