// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 已启用租户版本列表缓存项（平台级，全平台共享）。
/// </summary>
/// <remarks>版本目录是平台数据，各租户同一份：不再叠加框架的物理租户前缀，按模式整体失效才能覆盖所有租户。</remarks>
[IgnoreMultiTenancy]
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
