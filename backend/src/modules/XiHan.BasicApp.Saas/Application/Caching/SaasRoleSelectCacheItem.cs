#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasRoleSelectCacheItem
// Guid:c2e5a8b3-4d6f-4b97-8a23-1f4b0d6e9c57
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
