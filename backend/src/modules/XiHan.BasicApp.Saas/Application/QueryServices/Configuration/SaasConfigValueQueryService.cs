#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasConfigValueQueryService
// Guid:e2fdc986-15ec-4bec-8dc8-e343d31d87e2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// SaaS 配置值查询服务实现
/// </summary>
public sealed class SaasConfigValueQueryService
    : ISaasConfigValueQueryService
{
    private readonly IConfigRepository _configRepository;
    private readonly IDistributedCache<SaasConfigValueCacheItem, string> _configValueCache;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasConfigValueQueryService(
        IConfigRepository configRepository,
        IDistributedCache<SaasConfigValueCacheItem, string> configValueCache,
        ICurrentTenant currentTenant)
    {
        _configRepository = configRepository;
        _configValueCache = configValueCache;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public async Task<SaasConfigValueCacheItem> GetValueItemAsync(string configKey, CancellationToken cancellationToken = default)
    {
        var normalizedKey = SaasConfigKeys.Normalize(configKey);
        var cacheKey = SaasCacheKeys.ConfigValue(_currentTenant.Id, normalizedKey);
        return await _configValueCache.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var config = await _configRepository.GetEffectiveByKeyAsync(normalizedKey, _currentTenant.Id, cancellationToken);
                    return new SaasConfigValueCacheItem
                    {
                        ConfigKey = normalizedKey,
                        Value = ResolveValue(config),
                        DataType = config?.DataType ?? ConfigDataType.String,
                        Exists = config is not null,
                        CachedAt = DateTimeOffset.UtcNow
                    };
                },
                CreateCacheOptions,
                hideErrors: true,
                token: cancellationToken)
            ?? new SaasConfigValueCacheItem
            {
                ConfigKey = normalizedKey,
                Exists = false,
                CachedAt = DateTimeOffset.UtcNow
            };
    }

    private static DistributedCacheEntryOptions CreateCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
    }

    private static string? ResolveValue(SysConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(config.ConfigValue)
            ? config.DefaultValue
            : config.ConfigValue;
    }
}
