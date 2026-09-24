// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 启用岗位选择项缓存项。
/// </summary>
/// <remarks>逻辑键已含租户维度：不再叠加框架的物理租户前缀，按模式整体失效才能覆盖所有租户。</remarks>
[IgnoreMultiTenancy]
[CacheName(SaasCacheNames.PositionSelect)]
public sealed class SaasPositionSelectCacheItem
{
    /// <summary>
    /// 岗位选择项集合。
    /// </summary>
    public List<PositionListItemDto> Items { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
