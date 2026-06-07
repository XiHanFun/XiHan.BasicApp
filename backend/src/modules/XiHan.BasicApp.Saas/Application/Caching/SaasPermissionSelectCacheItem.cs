#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasPermissionSelectCacheItem
// Guid:b1d4f7a2-3c5e-4a86-9f12-7e3a9c0d5b48
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 可选全局权限选择项缓存项。
/// </summary>
[CacheName(SaasCacheNames.PermissionSelect)]
public sealed class SaasPermissionSelectCacheItem
{
    /// <summary>
    /// 权限选择项集合。
    /// </summary>
    public List<PermissionSelectItemDto> Items { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
