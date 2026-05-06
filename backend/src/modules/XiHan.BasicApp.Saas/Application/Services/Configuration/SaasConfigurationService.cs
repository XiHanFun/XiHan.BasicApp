#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasConfigurationService
// Guid:b22d6220-2c26-4738-ab7b-c25aaefeffa1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Distributed;
using System.Globalization;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// SaaS 运行时配置服务实现。
/// </summary>
public sealed class SaasConfigurationService(
    IConfigRepository configRepository,
    IDistributedCache<SaasConfigValueCacheItem, string> configValueCache,
    ICurrentTenant currentTenant)
    : ISaasConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConfigRepository _configRepository = configRepository;
    private readonly IDistributedCache<SaasConfigValueCacheItem, string> _configValueCache = configValueCache;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <inheritdoc />
    public async Task<string?> GetStringAsync(string configKey, string? defaultValue = null, CancellationToken cancellationToken = default)
    {
        var item = await GetValueItemAsync(configKey, cancellationToken);
        return item.Exists ? item.Value ?? defaultValue : defaultValue;
    }

    /// <inheritdoc />
    public async Task<bool> GetBooleanAsync(string configKey, bool defaultValue = false, CancellationToken cancellationToken = default)
    {
        var value = await GetStringAsync(configKey, defaultValue.ToString(CultureInfo.InvariantCulture), cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "y" or "on" => true,
            "0" or "false" or "no" or "n" or "off" => false,
            _ => defaultValue
        };
    }

    /// <inheritdoc />
    public async Task<int> GetInt32Async(string configKey, int defaultValue = 0, CancellationToken cancellationToken = default)
    {
        var value = await GetStringAsync(configKey, defaultValue.ToString(CultureInfo.InvariantCulture), cancellationToken);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetStringListAsync(string configKey, IReadOnlyList<string> defaultValue, CancellationToken cancellationToken = default)
    {
        var value = await GetStringAsync(configKey, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith("[", StringComparison.Ordinal))
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(normalized, JsonOptions) ?? [];
            return parsed
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .Select(static item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var items = normalized
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return items.Count == 0 ? defaultValue : items;
    }

    /// <inheritdoc />
    public async Task<LoginConfigDto> GetLoginConfigAsync(CancellationToken cancellationToken = default)
    {
        var loginMethods = await GetStringListAsync(SaasConfigKeys.Auth.LoginMethods, ["password"], cancellationToken);
        var tenantEnabled = await GetBooleanAsync(SaasConfigKeys.Auth.TenantSelectionEnabled, true, cancellationToken);
        var oauthProviders = await GetOAuthProvidersAsync(cancellationToken);

        return new LoginConfigDto
        {
            LoginMethods = loginMethods.Count == 0 ? ["password"] : [.. loginMethods],
            TenantEnabled = tenantEnabled,
            OAuthProviders = oauthProviders
        };
    }

    private async Task<List<OAuthProviderItemDto>> GetOAuthProvidersAsync(CancellationToken cancellationToken)
    {
        var value = await GetStringAsync(SaasConfigKeys.Auth.OAuthProviders, "[]", cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<OAuthProviderItemDto>>(value, JsonOptions)?
                .Where(static provider => !string.IsNullOrWhiteSpace(provider.Name))
                .Select(static provider => new OAuthProviderItemDto
                {
                    Name = provider.Name.Trim(),
                    DisplayName = string.IsNullOrWhiteSpace(provider.DisplayName) ? provider.Name.Trim() : provider.DisplayName.Trim()
                })
                .ToList() ?? [];
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"配置 {SaasConfigKeys.Auth.OAuthProviders} 必须是 OAuth 提供商 JSON 数组。", exception);
        }
    }

    private async Task<SaasConfigValueCacheItem> GetValueItemAsync(string configKey, CancellationToken cancellationToken)
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
