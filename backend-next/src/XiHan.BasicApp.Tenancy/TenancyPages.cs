// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Tenancy;

/// <summary>
/// 租户包的页面登记（权限只在平台侧，租户主体的菜单树里自然没有）。
/// </summary>
public static class TenancyPages
{
    /// <summary>
    /// 租户页。
    /// </summary>
    public static PageDescriptor Tenants { get; } = new("tenancy.tenants", "/tenancy/tenants", "tenancy.tenants.title", "lucide:building", 300, TenancyPermissions.TenantsRead);

    /// <summary>
    /// 套餐页。
    /// </summary>
    public static PageDescriptor Editions { get; } = new("tenancy.editions", "/tenancy/editions", "tenancy.editions.title", "lucide:package", 310, TenancyPermissions.EditionsRead);

    /// <summary>
    /// 全部顶层页面。
    /// </summary>
    /// <summary>
    /// 我的订阅页（只在租户侧）。
    /// </summary>
    public static PageDescriptor Subscription { get; } = new("tenancy.subscription", "/tenancy/subscription", "tenancy.subscription.title", "lucide:receipt", 320, TenancyPermissions.SubscriptionRead);

    /// <summary>
    /// 全部顶层页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Tenants, Editions, Subscription];
}
