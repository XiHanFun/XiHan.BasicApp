// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.update</c> 的请求：改名称、说明、排序与状态（角色码不可改）。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">角色 ID。</param>
/// <param name="Name">显示名。</param>
/// <param name="Description">说明，可空。</param>
/// <param name="Order">排序号。</param>
/// <param name="Status">状态；停用的角色不进入成员的令牌。</param>
public sealed record UpdateRoleRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string Name,
    [StringLength(256)] string? Description = null,
    [Range(0, 100_000)] int Order = 0,
    RoleStatus Status = RoleStatus.Active);
