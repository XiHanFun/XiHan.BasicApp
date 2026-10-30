// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Contracts.Caching;

/// <summary>
/// 一个能力包登记的缓存键命名空间。
/// </summary>
/// <remarks>
/// 缓存管理（Maintenance）只开放登记过的前缀下的键，框架自己的键（如分布式锁）不出现。
/// 鉴权相关的命名空间（授权、身份、开放平台签名、套餐门控）登记为不可改写（4.x 同样禁止改写），删除仍可（删了只是让缓存按需重建）。
/// </remarks>
/// <param name="Package">包名（小写）。</param>
/// <param name="Prefix">键前缀，以 <c>:</c> 结尾，如 <c>access:</c>。</param>
/// <param name="Editable">能否在缓存管理里改写值。</param>
public sealed record CacheNamespace(string Package, string Prefix, bool Editable);

/// <summary>
/// 缓存键命名空间的登记。
/// </summary>
public static class CacheNamespaceServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个包的缓存键命名空间。
    /// </summary>
    /// <remarks>
    /// 前缀是包名加 <c>:</c>。
    /// </remarks>
    public static IServiceCollection AddCacheNamespace(this IServiceCollection services, string package, bool editable)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(package);
        return services.AddSingleton(new CacheNamespace(package, package + ":", editable));
    }
}
