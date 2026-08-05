// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 权限全量目录缓存项。
/// </summary>
[CacheName(SaasCacheNames.PermissionCatalog)]
public sealed class SaasPermissionCatalogCacheItem
{
    /// <summary>
    /// 权限目录集合。
    /// </summary>
    public List<PermissionListItemDto> Items { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
