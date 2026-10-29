// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// <c>tenants.get</c> 的响应：租户、管理员登录名与用户数。
/// </summary>
/// <param name="Tenant">租户。</param>
/// <param name="AdminUserName">租户管理员登录名；未初始化为空。</param>
/// <param name="UserCount">注册地是该租户的用户数。</param>
/// <param name="Quota">有效配额与用量（席位、存储）。</param>
public sealed record TenantDetailResponse(TenantResponse Tenant, string? AdminUserName, long UserCount, TenantQuotaResponse Quota);
