// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 版本门控缓存项（租户 → 版本权限白名单；鉴权快照每请求读取的热路径）。
/// </summary>
[IgnoreMultiTenancy]
[CacheName(SaasCacheNames.EditionGate)]
public sealed class SaasEditionGateCacheItem
{
    /// <summary>
    /// 租户绑定的版本标识（未绑定为 null，表示不门控）。
    /// </summary>
    public long? EditionId { get; set; }

    /// <summary>
    /// 版本权限白名单（有效映射的权限主键集合；空集合表示版本未配置白名单，不门控）。
    /// </summary>
    public List<long> PermissionIds { get; set; } = [];

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
