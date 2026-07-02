#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasConfigValueCacheItem
// Guid:c5942bf0-4950-4684-a948-821647ce6d30
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 配置值缓存项。
/// </summary>
[IgnoreMultiTenancy]
[CacheName(SaasCacheNames.ConfigValue)]
public sealed class SaasConfigValueCacheItem
{
    /// <summary>
    /// 配置键。
    /// </summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置值。
    /// </summary>
    /// <remarks>
    /// 缓存内存的是「原始值」：加密配置（<see cref="IsEncrypted"/> 为 true）存密文，取出后再解密（最小泄漏面）；
    /// 回退 <c>DefaultValue</c> 的值恒为明文（默认值不参与加密，加密配置不应设默认值）。
    /// </remarks>
    public string? Value { get; set; }

    /// <summary>
    /// 值是否为密文（true 表示 <see cref="Value"/> 需经配置值保护器解密后使用）。
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// 数据类型。
    /// </summary>
    public ConfigDataType DataType { get; set; } = ConfigDataType.String;

    /// <summary>
    /// 配置是否存在且启用。
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
