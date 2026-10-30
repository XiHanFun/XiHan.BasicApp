// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// <c>tenants.transfer-owner</c> 的请求：把租户所有权转给另一个用户。
/// </summary>
/// <remarks>
/// 标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">租户主键。</param>
/// <param name="UserId">新所有者：该租户注册地的用户或生效中的普通成员，启用且未锁定。</param>
public sealed record TransferOwnerRequest(Guid Id, Guid UserId);
