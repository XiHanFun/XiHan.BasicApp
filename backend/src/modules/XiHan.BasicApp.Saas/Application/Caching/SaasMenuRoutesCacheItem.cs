// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 菜单路由缓存项。
/// </summary>
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
