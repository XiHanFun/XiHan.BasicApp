// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.BasicApp.Settings.Operations.Dicts;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Settings.Infrastructure;

/// <summary>
/// 字典选项的读穿缓存（单例）
/// </summary>
/// <remarks>
/// 按（侧、编码）各存这一侧那本字典的选项，租户读时先看自己的、再看平台的：平台字典改了只失效平台那一份
/// 停用的字典存空选项，这一侧没有这本字典存空串。放在共享缓存里 10 分钟，字典与字典项写入即失效
/// </remarks>
/// <param name="cache">共享缓存</param>
/// <param name="scopes">作用域工厂（缓存未命中时用只读工作单元查主库）</param>
public sealed class DictOptionSets(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 一侧一本字典的选项
    /// </summary>
    /// <returns>这一侧没有这个编码的字典即 <c>null</c></returns>
    public async Task<DictOptionsResponse?> OfAsync(string owner, string code, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner, code), ct => LoadAsync(owner, code, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return raw.Length == 0 ? null : JsonSerializer.Deserialize(raw, SettingsJsonContext.Default.DictOptionsResponse);
    }

    /// <summary>
    /// 一侧一本字典的选项失效
    /// </summary>
    /// <remarks>
    /// 调用里推到工作单元结束之后再删除（<see cref="CacheInvalidation"/>），提交前的旧数据不会被回填进缓存
    /// </remarks>
    public async Task InvalidateAsync(string owner, string code, CancellationToken cancellationToken)
        => await CacheInvalidation.RemoveAsync(cache, KeyOf(owner, code), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string owner, string code) => new($"settings:dict-options:{owner}:{code}");

    private async ValueTask<string> LoadAsync(string owner, string code, CancellationToken cancellationToken)
        => await StoreScope.ReadPrimaryAsync(scopes, async (services, ct) =>
        {
            var dicts = await services.GetRequiredService<IRepository<SysDict, Guid>>()
                .ListAsync(EntityQuery.All<SysDict>().Where(new Specification<SysDict>(d => d.OwnerTenantId == owner && d.Code == code)).Page(0, 1), ct)
                .ConfigureAwait(false);
            if (dicts.Count == 0)
            {
                return string.Empty;
            }

            var dict = dicts[0];
            if (dict.Status != SettingStatus.Active)
            {
                return JsonSerializer.Serialize(new DictOptionsResponse([]), SettingsJsonContext.Default.DictOptionsResponse);
            }

            var all = await services.GetRequiredService<DictService>().ItemsOfAsync(dict.Id, ct).ConfigureAwait(false);
            var codes = all.ToDictionary(static i => i.Id, static i => i.Code);
            var options = DictService.Flatten(all)
                .Select(entry => new DictOption(
                    entry.Item.Code,
                    entry.Item.Name,
                    entry.Item.ParentId is { } parentId && codes.TryGetValue(parentId, out var parentCode) ? parentCode : null,
                    entry.Item.IsDefault,
                    entry.Item.Status != SettingStatus.Active))
                .ToList();
            return JsonSerializer.Serialize(new DictOptionsResponse(options), SettingsJsonContext.Default.DictOptionsResponse);
        }, cancellationToken).ConfigureAwait(false);
}
