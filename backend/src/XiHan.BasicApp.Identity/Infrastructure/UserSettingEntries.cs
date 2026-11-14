// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Operations.UserSettings;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 用户设置的读穿缓存（单例）
/// </summary>
/// <remarks>
/// 按（用户、场景、设置键）各存一条，还没存过的也缓存成空值：前端每进一个页面都要读偏好与页面设置
/// 放在共享缓存里 10 分钟，写入即失效
/// </remarks>
/// <param name="cache">共享缓存</param>
/// <param name="scopes">作用域工厂（缓存未命中时用只读工作单元查主库）</param>
public sealed class UserSettingEntries(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 一个用户的一条设置
    /// </summary>
    /// <returns>还没存过即值与时间为空</returns>
    public async Task<UserSettingResponse> OfAsync(Guid userId, UserSettingScene scene, string key, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(userId, scene, key), ct => LoadAsync(userId, scene, key, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(raw, IdentityJsonContext.Default.UserSettingResponse) ?? new UserSettingResponse(scene, key, null, null);
    }

    /// <summary>
    /// 一个用户的一条设置失效
    /// </summary>
    /// <remarks>
    /// 调用里推到工作单元结束之后再删除（<see cref="CacheInvalidation"/>），提交前的旧数据不会被回填进缓存
    /// </remarks>
    public async Task InvalidateAsync(Guid userId, UserSettingScene scene, string key, CancellationToken cancellationToken)
        => await CacheInvalidation.RemoveAsync(cache, KeyOf(userId, scene, key), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(Guid userId, UserSettingScene scene, string key) => new($"identity:user-settings:{userId:N}:{scene}:{key}");

    private async ValueTask<string> LoadAsync(Guid userId, UserSettingScene scene, string key, CancellationToken cancellationToken)
        => await StoreScope.ReadPrimaryAsync(scopes, async (services, ct) =>
        {
            var rows = await services.GetRequiredService<IRepository<SysUserSetting, Guid>>()
                .ListAsync(EntityQuery.All<SysUserSetting>().Where(new Specification<SysUserSetting>(s => s.UserId == userId && s.Scene == scene && s.Key == key)).Page(0, 1), ct)
                .ConfigureAwait(false);
            var response = rows.Count == 0
                ? new UserSettingResponse(scene, key, null, null)
                : new UserSettingResponse(scene, key, rows[0].Value, rows[0].ModifiedAt ?? rows[0].CreatedAt);
            return JsonSerializer.Serialize(response, IdentityJsonContext.Default.UserSettingResponse);
        }, cancellationToken).ConfigureAwait(false);
}
