// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// <c>tenants.get</c> 的请求：标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">租户主键。</param>
public sealed record GetTenantRequest(Guid Id);
