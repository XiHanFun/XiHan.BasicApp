#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasMenuRoutesCacheItem
// Guid:35d38916-395f-46fc-9b1a-32f6319fed56
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
