// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Organization.Operations.Memberships;

/// <summary>
/// <c>memberships.create</c> 的请求：把用户加进部门。
/// </summary>
/// <param name="UserId">用户（注册地必须是当前租户）。</param>
/// <param name="DepartmentId">部门。</param>
/// <param name="PositionId">岗位。</param>
/// <param name="IsPrimary">设为主部门；用户的第一个归属自动是主部门。</param>
/// <param name="EmployeeNumber">工号。</param>
/// <param name="JobGrade">职级。</param>
/// <param name="HiredAt">入职时间（UTC）。</param>
/// <param name="Order">部门内排序号。</param>
public sealed record CreateMembershipRequest(
    Guid UserId,
    Guid DepartmentId,
    Guid? PositionId = null,
    bool IsPrimary = false,
    [StringLength(64)] string? EmployeeNumber = null,
    [StringLength(64)] string? JobGrade = null,
    DateTimeOffset? HiredAt = null,
    [Range(0, 100_000)] int Order = 0);

/// <summary>
/// <c>memberships.update</c> 的请求。
/// </summary>
/// <remarks>
/// 用户与部门不在这里改（换部门即删了再加）。
/// </remarks>
/// <param name="Id">归属主键。</param>
/// <param name="PositionId">岗位；为空即清除。</param>
/// <param name="IsPrimary">设为主部门；主部门不能在这里取消（给另一条设主即可）。</param>
/// <param name="EmployeeNumber">工号。</param>
/// <param name="JobGrade">职级。</param>
/// <param name="HiredAt">入职时间（UTC）。</param>
/// <param name="Order">部门内排序号。</param>
public sealed record UpdateMembershipRequest(
    Guid Id,
    Guid? PositionId = null,
    bool IsPrimary = false,
    [StringLength(64)] string? EmployeeNumber = null,
    [StringLength(64)] string? JobGrade = null,
    DateTimeOffset? HiredAt = null,
    [Range(0, 100_000)] int Order = 0);

/// <summary>
/// <c>memberships.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">归属主键，1 到 100 个。</param>
public sealed record MembershipIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
