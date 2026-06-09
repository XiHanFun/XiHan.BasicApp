#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasUserSettingCacheItem
// Guid:9e501236-7f81-4d2e-9a5c-4b6f0d3e8c95
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 用户设置缓存项。
/// </summary>
/// <remarks>缓存键已编码 UserId（全局唯一），故忽略多租户键前缀。</remarks>
[IgnoreMultiTenancy]
[CacheName(SaasCacheNames.UserSetting)]
public sealed class SaasUserSettingCacheItem
{
    /// <summary>
    /// 设置场景。
    /// </summary>
    public UserSettingScene Scene { get; set; } = UserSettingScene.Preference;

    /// <summary>
    /// 设置键。
    /// </summary>
    public string SettingKey { get; set; } = string.Empty;

    /// <summary>
    /// 设置载荷（JSON）。
    /// </summary>
    public string? SettingValue { get; set; }

    /// <summary>
    /// 设置是否存在。
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
