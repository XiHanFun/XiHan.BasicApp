// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 用户授权快照缓存项。
/// </summary>
/// <remarks>逻辑键已含 用户 × 租户 维度：不再叠加框架的物理租户前缀，按模式整体失效才能覆盖所有租户。</remarks>
[IgnoreMultiTenancy]
[CacheName(SaasCacheNames.AuthorizationSnapshot)]
public sealed class SaasAuthorizationSnapshotCacheItem
{
    /// <summary>
    /// 用户标识。
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 角色编码集合。
    /// </summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>
    /// 权限编码集合。
    /// </summary>
    public List<string> Permissions { get; set; } = [];

    /// <summary>
    /// 权限标识集合。
    /// </summary>
    public List<long> PermissionIds { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
