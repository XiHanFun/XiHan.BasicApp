// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 已启用角色选择项缓存项。
/// </summary>
[CacheName(SaasCacheNames.RoleSelect)]
public sealed class SaasRoleSelectCacheItem
{
    /// <summary>
    /// 角色选择项集合。
    /// </summary>
    public List<RoleSelectItemDto> Items { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
