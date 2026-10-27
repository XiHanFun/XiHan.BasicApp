// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.grant / roles.revoke</c> 的请求：角色与一批权限码。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">角色 ID。</param>
/// <param name="Permissions">权限码，1 到 200 个。</param>
public sealed record RolePermissionsRequest(Guid Id, [Required, MinLength(1), MaxLength(200)] IReadOnlyList<string> Permissions);
