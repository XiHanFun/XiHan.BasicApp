#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasOperationSelectCacheItem
// Guid:f5b8d1e6-7a9c-4eca-9d56-4c7e3a9b2f80
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
