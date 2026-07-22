// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 会话状态缓存项（session_id → 有效性 + 锁定位；会话闸门每请求读取的热路径）。
/// </summary>
/// <remarks>
/// <see cref="IgnoreMultiTenancy"/>：会话闸门跑在租户解析之后但服务于全租户，键本身即 session_id（全局唯一），
/// 不按租户分片，避免同一会话在不同租户上下文下缓存出多份。
/// </remarks>
[IgnoreMultiTenancy]
[CacheName(SaasCacheNames.SessionState)]
public sealed class SaasSessionStateCacheItem
{
    /// <summary>
    /// 会话是否存在（false 表示查无此会话——用于缓存"不存在"这一事实，避免每请求穿透查库）。
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// 会话所属用户主键。
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 会话状态（仅 <see cref="SessionStatus.Active"/> 视为有效）。
    /// </summary>
    public SessionStatus Status { get; set; }

    /// <summary>
    /// 是否锁定（锁屏只是原因之一，见 <see cref="LockReason"/>）。
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// 锁定原因（<see cref="SessionLockReasons"/>；经 423 响应体透传给前端，决定引导哪种解锁方式）。
    /// </summary>
    public string? LockReason { get; set; }

    /// <summary>
    /// 会话过期时间（超过即视为失效）。
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 展示名（解锁页要显示"锁的是谁"；此时用户信息接口本身是被 423 挡住的）。
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 头像地址。
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
