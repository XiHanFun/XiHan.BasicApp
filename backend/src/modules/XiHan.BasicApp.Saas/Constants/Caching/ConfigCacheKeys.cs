#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigCacheKeys
// Guid:6dab4c5e-bf7a-4b8d-ce9f-4a5b6c7d8e9f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Caching;

/// <summary>
/// 配置模块缓存键
/// </summary>
public static class ConfigCacheKeys
{
    private const string Module = "Saas:Config";

    /// <summary>
    /// 按键查询模板（[Cacheable] 用；占位符须与方法参数名一致）
    /// </summary>
    public const string ByKeyTemplate = $"{Module}:Key:{{tenantId}}:{{configKey}}";

    /// <summary>
    /// 按分组查询模板（[Cacheable] 用）
    /// </summary>
    public const string ByGroupTemplate = $"{Module}:Group:{{tenantId}}:{{configGroup}}";

    /// <summary>
    /// 按键构建缓存键
    /// </summary>
    public static string ByKey(long? tenantId, string configKey)
        => $"{Module}:Key:{FormatTenant(tenantId)}:{configKey}";

    /// <summary>
    /// 按分组构建缓存键
    /// </summary>
    public static string ByGroup(long? tenantId, string? configGroup)
        => $"{Module}:Group:{FormatTenant(tenantId)}:{configGroup ?? "Default"}";

    private static string FormatTenant(long? tenantId) => tenantId?.ToString() ?? "Host";
}
