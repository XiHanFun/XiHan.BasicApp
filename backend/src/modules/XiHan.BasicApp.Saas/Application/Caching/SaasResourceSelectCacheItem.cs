#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasResourceSelectCacheItem
// Guid:e4a7c0d5-6f8b-4db9-8c45-3b6d2f8a1e79
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 可选全局资源选择项缓存项。
/// </summary>
[CacheName(SaasCacheNames.ResourceSelect)]
public sealed class SaasResourceSelectCacheItem
{
    /// <summary>
    /// 资源选择项集合。
    /// </summary>
    public List<ResourceSelectItemDto> Items { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
