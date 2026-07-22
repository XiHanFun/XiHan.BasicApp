// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 可选全局操作选择项缓存项。
/// </summary>
[CacheName(SaasCacheNames.OperationSelect)]
public sealed class SaasOperationSelectCacheItem
{
    /// <summary>
    /// 操作选择项集合。
    /// </summary>
    public List<OperationSelectItemDto> Items { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
