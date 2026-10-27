// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Organization.Domain.Entities;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// <c>departments.create</c> 的请求。
/// </summary>
/// <param name="Code">编码（租户内唯一）。</param>
/// <param name="Name">名称。</param>
/// <param name="ParentId">直接上级；为空即顶层。</param>
/// <param name="LeaderUserId">负责人（本租户的用户）。</param>
/// <param name="Order">同级排序号。</param>
/// <param name="Description">说明。</param>
public sealed record CreateDepartmentRequest(
    [Required, StringLength(64, MinimumLength = 1), RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")] string Code,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    Guid? ParentId = null,
    Guid? LeaderUserId = null,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(512)] string? Description = null);

/// <summary>
/// <c>departments.get</c> 的请求。
/// </summary>
/// <param name="Id">部门主键。</param>
public sealed record GetDepartmentRequest(Guid Id);

/// <summary>
/// <c>departments.update</c> 的请求：编码与上级不在这里改（上级走 <c>departments.move</c>）。
/// </summary>
/// <param name="Id">部门主键。</param>
/// <param name="Name">名称。</param>
/// <param name="LeaderUserId">负责人；为空即清除。</param>
/// <param name="Order">同级排序号。</param>
/// <param name="Description">说明。</param>
/// <param name="Status">状态。</param>
public sealed record UpdateDepartmentRequest(
    Guid Id,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    Guid? LeaderUserId = null,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(512)] string? Description = null,
    OrganizationStatus Status = OrganizationStatus.Active);

/// <summary>
/// <c>departments.move</c> 的请求。
/// </summary>
/// <param name="Id">部门主键。</param>
/// <param name="ParentId">新上级；为空即移到顶层。</param>
public sealed record MoveDepartmentRequest(Guid Id, Guid? ParentId = null);

/// <summary>
/// <c>departments.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">部门主键，1 到 100 个。</param>
public sealed record DepartmentIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
