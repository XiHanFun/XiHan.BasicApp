// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户设置查询应用服务（读侧：按 用户 × 场景 × 设置键 读取，带分布式缓存；写后由失效器整体失效该用户设置）。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户设置")]
public sealed class UserSettingQueryService
    : SaasApplicationService, IUserSettingQueryService
{
    private readonly ICurrentUser _currentUser;

    private readonly IUserSettingRepository _repository;

    private readonly IDistributedCache<SaasUserSettingCacheItem, string> _userSettingCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSettingQueryService(
        IUserSettingRepository repository,
        IDistributedCache<SaasUserSettingCacheItem, string> userSettingCache,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _userSettingCache = userSettingCache;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<UserSettingDto> GetAsync(UserSettingScene scene, string settingKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settingKey))
        {
            throw new ArgumentException("设置键不能为空。", nameof(settingKey));
        }

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var normalizedKey = settingKey.Trim();
        var cacheKey = SaasCacheKeys.UserSetting(userId, scene, normalizedKey);

        var item = await _userSettingCache.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var entity = await _repository.GetByUserSettingAsync(userId, scene, normalizedKey, cancellationToken);
                    return new SaasUserSettingCacheItem
                    {
                        Scene = scene,
                        SettingKey = normalizedKey,
                        SettingValue = entity?.SettingValue,
                        Exists = entity is not null,
                        CachedAt = DateTimeOffset.UtcNow
                    };
                },
                CreateCacheOptions,
                hideErrors: true,
                token: cancellationToken)
            ?? new SaasUserSettingCacheItem
            {
                Scene = scene,
                SettingKey = normalizedKey,
                Exists = false,
                CachedAt = DateTimeOffset.UtcNow
            };

        return new UserSettingDto
        {
            Scene = item.Scene,
            SettingKey = item.SettingKey,
            SettingValue = item.SettingValue
        };
    }

    private static DistributedCacheEntryOptions CreateCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
    }
}
