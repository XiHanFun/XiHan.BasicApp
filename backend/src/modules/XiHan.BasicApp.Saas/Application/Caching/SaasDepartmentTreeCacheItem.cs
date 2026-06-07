#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasDepartmentTreeCacheItem
// Guid:a6c9e2f7-8b0d-4fdb-9e67-5d8f4a0c3b91
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
