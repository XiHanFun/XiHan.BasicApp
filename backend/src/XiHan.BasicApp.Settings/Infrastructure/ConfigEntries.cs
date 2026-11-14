// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Settings.Infrastructure;

/// <summary>
/// 一侧一个键的启用参数
/// </summary>
/// <param name="Value">存着的值（加密的是密文）</param>
/// <param name="IsEncrypted">是否加密</param>
/// <param name="DefaultValue">缺省值</param>
public sealed record ConfigEntry(string? Value, bool IsEncrypted, string? DefaultValue);

/// <summary>
/// 参数的读穿缓存（单例）
/// </summary>
/// <remarks>
/// 按（侧、键）各存这一侧启用的那条，租户读时先看自己的、再看平台的：平台参数改了只失效平台那一份，不用逐个租户失效
/// 加密的值存密文、读出再解密，明文不进缓存。放在共享缓存里 10 分钟，写入即失效
/// </remarks>
/// <param name="cache">共享缓存</param>
/// <param name="scopes">作用域工厂（缓存未命中时用只读工作单元查主库）</param>
public sealed class ConfigEntries(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 一侧一个键的启用参数
    /// </summary>
    /// <returns>这一侧没有启用的这个键即 <c>null</c></returns>
    public async Task<ConfigEntry?> OfAsync(string owner, string key, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner, key), ct => LoadAsync(owner, key, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return raw.Length == 0 ? null : JsonSerializer.Deserialize(raw, SettingsJsonContext.Default.ConfigEntry);
    }

    /// <summary>
    /// 一侧一个键的参数失效
    /// </summary>
    /// <remarks>
    /// 调用里推到工作单元结束之后再删除（<see cref="CacheInvalidation"/>），提交前的旧数据不会被回填进缓存
    /// </remarks>
    public async Task InvalidateAsync(string owner, string key, CancellationToken cancellationToken)
        => await CacheInvalidation.RemoveAsync(cache, KeyOf(owner, key), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string owner, string key) => new($"settings:configs:{owner}:{key}");

    private async ValueTask<string> LoadAsync(string owner, string key, CancellationToken cancellationToken)
        => await StoreScope.ReadPrimaryAsync(scopes, async (services, ct) =>
        {
            var rows = await services.GetRequiredService<IRepository<SysConfig, Guid>>()
                .ListAsync(EntityQuery.All<SysConfig>().Where(new Specification<SysConfig>(c => c.OwnerTenantId == owner && c.Key == key && c.Status == SettingStatus.Active)).Page(0, 1), ct)
                .ConfigureAwait(false);
            return rows.Count == 0
                ? string.Empty
                : JsonSerializer.Serialize(new ConfigEntry(rows[0].Value, rows[0].IsEncrypted, rows[0].DefaultValue), SettingsJsonContext.Default.ConfigEntry);
        }, cancellationToken).ConfigureAwait(false);
}
