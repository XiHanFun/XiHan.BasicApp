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
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// SaaS 配置值查询服务实现
/// </summary>
/// <remarks>
/// 加密约定：缓存内存的是原始（密文）值 + 加密标志，取出后再解密（密文进缓存，最小泄漏面）；
/// 加密配置回退 <c>DefaultValue</c> 时按明文使用（默认值不参与加密，加密配置不应设默认值）。
/// 解密失败即抛异常（fail-closed，不吐可疑值）。
/// </remarks>
public sealed class SaasConfigValueQueryService
    : ISaasConfigValueQueryService
{
    private readonly IConfigRepository _configRepository;
    private readonly IDistributedCache<SaasConfigValueCacheItem, string> _configValueCache;
    private readonly ICurrentTenant _currentTenant;
    private readonly IConfigValueSecretProtector _configValueSecretProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasConfigValueQueryService(
        IConfigRepository configRepository,
        IDistributedCache<SaasConfigValueCacheItem, string> configValueCache,
        ICurrentTenant currentTenant,
        IConfigValueSecretProtector configValueSecretProtector)
    {
        _configRepository = configRepository;
        _configValueCache = configValueCache;
        _currentTenant = currentTenant;
        _configValueSecretProtector = configValueSecretProtector;
    }

    /// <inheritdoc />
    public async Task<SaasConfigValueCacheItem> GetValueItemAsync(string configKey, CancellationToken cancellationToken = default)
    {
        var normalizedKey = SaasConfigKeys.Normalize(configKey);
        var cacheKey = SaasCacheKeys.ConfigValue(_currentTenant.Id, normalizedKey);
        var item = await _configValueCache.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var config = await _configRepository.GetEffectiveByKeyAsync(normalizedKey, _currentTenant.Id, cancellationToken);
                    var useCurrentValue = !string.IsNullOrWhiteSpace(config?.ConfigValue);
                    return new SaasConfigValueCacheItem
                    {
                        ConfigKey = normalizedKey,
                        // 原始值进缓存：加密行存密文；回退默认值恒为明文
                        Value = useCurrentValue ? config!.ConfigValue : config?.DefaultValue,
                        IsEncrypted = useCurrentValue && (config?.IsEncrypted ?? false),
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

        if (!item.IsEncrypted)
        {
            return item;
        }

        // 取出后解密（返回副本，不回写缓存实例）；解密失败即抛（fail-closed）
        return new SaasConfigValueCacheItem
        {
            ConfigKey = item.ConfigKey,
            Value = _configValueSecretProtector.Unprotect(item.Value),
            IsEncrypted = false,
            DataType = item.DataType,
            Exists = item.Exists,
            CachedAt = item.CachedAt
        };
    }

    private static DistributedCacheEntryOptions CreateCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
    }
}
