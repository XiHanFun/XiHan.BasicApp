// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// <c>users.get</c> 的请求：只有标识，由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">用户 ID。</param>
public sealed record GetUserRequest(Guid Id);
