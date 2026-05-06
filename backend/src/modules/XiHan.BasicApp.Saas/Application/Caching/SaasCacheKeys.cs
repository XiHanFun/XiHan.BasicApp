#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasCacheKeys
// Guid:5026e4bd-d6fe-43f8-9c3d-530e920b0210
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Saas.Domain.Configurations;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 模块业务缓存键构造器。
/// </summary>
public static class SaasCacheKeys
{
    /// <summary>
    /// 配置值缓存键。
    /// </summary>
    /// <param name="tenantId">租户标识。</param>
    /// <param name="configKey">配置键。</param>
    /// <returns>业务缓存键。</returns>
    public static string ConfigValue(long? tenantId, string configKey)
    {
        var tenantSegment = tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value.ToString() : "platform";
        return $"tenant:{tenantSegment}:key:{SaasConfigKeys.Normalize(configKey)}";
    }

    /// <summary>
    /// 指定配置键的所有租户缓存匹配模式。
    /// </summary>
    /// <param name="configKey">配置键。</param>
    /// <returns>业务缓存键匹配模式。</returns>
    public static string ConfigValuePattern(string configKey)
    {
        return $"tenant:*:key:{SaasConfigKeys.Normalize(configKey)}";
    }

    /// <summary>
    /// 所有配置值缓存匹配模式。
    /// </summary>
    public static string AllConfigValuesPattern()
    {
        return "tenant:*:key:*";
    }

    /// <summary>
    /// 用户授权快照缓存键。
    /// </summary>
    /// <param name="userId">用户标识。</param>
    /// <returns>业务缓存键。</returns>
    public static string AuthorizationSnapshot(long userId)
    {
        return $"user:{userId}";
    }

    /// <summary>
    /// 菜单路由缓存键。
    /// </summary>
    /// <param name="permissionIds">权限标识集合。</param>
    /// <param name="hasAllPermissions">是否拥有全部权限。</param>
    /// <returns>业务缓存键。</returns>
    public static string MenuRoutes(IEnumerable<long> permissionIds, bool hasAllPermissions)
    {
        var source = hasAllPermissions
            ? "*"
            : string.Join(',', permissionIds.Distinct().OrderBy(static id => id));
        return $"permission-set:{Hash(source)}";
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..24].ToLowerInvariant();
    }
}
