// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 菜单路由缓存项。
/// </summary>
/// <remarks>菜单是平台目录，路由只由权限集合决定，各租户同一份：不再叠加框架的物理租户前缀，按模式整体失效才能覆盖所有租户。</remarks>
[IgnoreMultiTenancy]
[CacheName(SaasCacheNames.MenuRoutes)]
public sealed class SaasMenuRoutesCacheItem
{
    /// <summary>
    /// 菜单路由。
    /// </summary>
    public List<MenuRouteDto> Routes { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
