// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 部门树缓存项（按租户隔离）。
/// </summary>
[CacheName(SaasCacheNames.DepartmentTree)]
public sealed class SaasDepartmentTreeCacheItem
{
    /// <summary>
    /// 部门树根节点集合（含子节点）。
    /// </summary>
    public List<DepartmentTreeNodeDto> Nodes { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
