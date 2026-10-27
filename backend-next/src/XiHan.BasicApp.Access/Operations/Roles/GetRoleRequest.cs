// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.get</c> 的请求：标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">角色 ID。</param>
public sealed record GetRoleRequest(Guid Id);
