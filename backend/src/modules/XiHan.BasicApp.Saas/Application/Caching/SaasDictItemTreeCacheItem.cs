// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 字典项树缓存项（键已含租户维度，禁用框架级租户前缀避免双重隔离）。
/// </summary>
[IgnoreMultiTenancy]
[CacheName(SaasCacheNames.DictItemTree)]
public sealed class SaasDictItemTreeCacheItem
{
    /// <summary>
    /// 字典项树节点。
    /// </summary>
    public List<DictItemTreeNodeDto> Nodes { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
