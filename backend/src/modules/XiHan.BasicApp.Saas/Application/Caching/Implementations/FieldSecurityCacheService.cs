#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldSecurityCacheService
// Guid:a76d8d4d-97a8-47bc-a79d-8b8a78aef6fb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 22:12:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Constants.Settings;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Application.Caching.Implementations;

public class FieldSecurityCacheService : IFieldSecurityCacheService
{
    private static readonly DistributedCacheEntryOptions VersionCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
    };

    private readonly IDistributedCache<FieldSecurityVersionCacheItem> _versionCache;
    private readonly IDistributedCache<FieldSecurityDecisionCacheItem> _decisionCache;
    private readonly IConfiguration _configuration;

    public FieldSecurityCacheService(
        IDistributedCache<FieldSecurityVersionCacheItem> versionCache,
        IDistributedCache<FieldSecurityDecisionCacheItem> decisionCache,
        IConfiguration configuration)
    {
        _versionCache = versionCache;
        _decisionCache = decisionCache;
        _configuration = configuration;
    }

    public async Task<FieldSecurityDecisionCacheItem?> GetDecisionAsync(
        long userId,
        long? tenantId,
        string resourceCode,
        IReadOnlyCollection<string> fieldNames,
        Func<CancellationToken, Task<FieldSecurityDecisionCacheItem>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (userId <= 0 || string.IsNullOrWhiteSpace(resourceCode))
        {
            return null;
        }

        var version = await GetVersionAsync(tenantId, cancellationToken);
        var fieldSignature = BuildFieldSignature(fieldNames);

        return await _decisionCache.GetOrAddAsync(
            SaasCacheKeys.FieldSecurityDecision(tenantId, userId, resourceCode, version, fieldSignature),
            () => factory(cancellationToken),
            optionsFactory: BuildCacheOptions,
            token: cancellationToken);
    }

    public async Task InvalidateAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        var key = SaasCacheKeys.FieldSecurityVersion(tenantId);
        var currentVersion = await GetVersionAsync(tenantId, cancellationToken);
        await _versionCache.SetAsync(
            key,
            new FieldSecurityVersionCacheItem { Version = currentVersion + 1 },
            options: VersionCacheOptions,
            considerUow: true,
            token: cancellationToken);
    }

    private DistributedCacheEntryOptions BuildCacheOptions()
    {
        var absoluteMinutes = Math.Clamp(
            _configuration.GetValue(SaasSettingKeys.Caching.FieldSecurityAbsoluteExpirationMinutes, 15),
            1,
            1440);
        var slidingMinutes = Math.Clamp(
            _configuration.GetValue(SaasSettingKeys.Caching.FieldSecuritySlidingExpirationMinutes, 5),
            1,
            absoluteMinutes);

        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(absoluteMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(slidingMinutes)
        };
    }

    private async Task<long> GetVersionAsync(long? tenantId, CancellationToken cancellationToken)
    {
        var item = await _versionCache.GetOrAddAsync(
            SaasCacheKeys.FieldSecurityVersion(tenantId),
            () => Task.FromResult(new FieldSecurityVersionCacheItem { Version = 1 }),
            optionsFactory: () => VersionCacheOptions,
            token: cancellationToken);

        return item?.Version > 0 ? item.Version : 1;
    }

    private static string BuildFieldSignature(IReadOnlyCollection<string> fieldNames)
    {
        var normalized = fieldNames
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        return normalized.Length == 0 ? "all" : string.Join(",", normalized);
    }
}
