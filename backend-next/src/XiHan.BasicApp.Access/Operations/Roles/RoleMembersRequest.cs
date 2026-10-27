// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.assign / roles.unassign</c> 的请求：角色与一批用户。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">角色 ID。</param>
/// <param name="UserIds">用户 ID，1 到 100 个；用户的注册地必须与角色同一侧。</param>
public sealed record RoleMembersRequest(Guid Id, [Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> UserIds);
