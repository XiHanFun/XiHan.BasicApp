// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 已启用租户版本列表缓存项（平台级，全平台共享）。
/// </summary>
[CacheName(SaasCacheNames.TenantEditions)]
public sealed class SaasEnabledEditionsCacheItem
{
    /// <summary>
    /// 已启用租户版本集合。
    /// </summary>
    public List<TenantEditionListItemDto> Items { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
