#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasEnabledEditionsCacheItem
// Guid:d3f6b9c4-5e7a-4ca8-9b34-2a5c1e7f0d68
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
