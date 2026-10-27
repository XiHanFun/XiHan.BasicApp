// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Organization.Operations.Memberships;

/// <summary>
/// 归属的网络契约：带用户、部门、岗位的名称。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">用户登录名。</param>
/// <param name="DisplayName">用户显示名。</param>
/// <param name="DepartmentId">部门。</param>
/// <param name="DepartmentName">部门名称。</param>
/// <param name="PositionId">岗位。</param>
/// <param name="PositionName">岗位名称。</param>
/// <param name="IsPrimary">是否主部门。</param>
/// <param name="EmployeeNumber">工号。</param>
/// <param name="JobGrade">职级。</param>
/// <param name="HiredAt">入职时间（UTC）。</param>
/// <param name="Order">部门内排序号。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
public sealed record MembershipResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string DisplayName,
    Guid DepartmentId,
    string DepartmentName,
    Guid? PositionId,
    string? PositionName,
    bool IsPrimary,
    string? EmployeeNumber,
    string? JobGrade,
    DateTimeOffset? HiredAt,
    int Order,
    DateTimeOffset CreatedAt);
