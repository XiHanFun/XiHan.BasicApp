// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户可进入的一个租户：有效成员关系与它所在的可进入租户
/// </summary>
/// <param name="Membership">有效成员关系</param>
/// <param name="Tenant">可进入的租户</param>
public sealed record AccessibleTenant(SysTenantUser Membership, SysTenant Tenant);
