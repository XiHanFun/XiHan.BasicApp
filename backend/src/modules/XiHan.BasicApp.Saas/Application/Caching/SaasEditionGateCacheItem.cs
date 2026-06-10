#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasEditionGateCacheItem
// Guid:0c158daa-e14c-47f8-d59a-cfdb40b11ed5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
