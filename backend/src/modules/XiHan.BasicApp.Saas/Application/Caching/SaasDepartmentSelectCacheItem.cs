// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 启用部门选择项缓存项。
/// </summary>
[CacheName(SaasCacheNames.DepartmentSelect)]
public sealed class SaasDepartmentSelectCacheItem
{
    /// <summary>
    /// 部门选择项集合。
    /// </summary>
    public List<DepartmentListItemDto> Items { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
