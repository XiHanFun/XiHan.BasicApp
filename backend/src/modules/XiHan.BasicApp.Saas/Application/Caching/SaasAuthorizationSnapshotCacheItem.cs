#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasAuthorizationSnapshotCacheItem
// Guid:5a828cd9-9cfb-4502-9433-5c9c1cf9126a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 用户授权快照缓存项。
/// </summary>
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
