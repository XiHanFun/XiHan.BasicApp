#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacOpenApiSecurityClientStore
// Guid:47ffbb7d-5b66-46bf-bde2-5e3fbbf1f9f4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/13 23:37:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Security;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Web.Api.Security.OpenApi;

namespace XiHan.BasicApp.Saas.Infrastructure.Authorization;

/// <summary>
/// RBAC OpenApi 客户端存储（复用 OAuth 应用 + 缓存）
/// </summary>
public class RbacOpenApiSecurityClientStore : IOpenApiSecurityClientStore
{
    private readonly IOAuthAppRepository _oauthAppRepository;
    private readonly IConfigRepository _configRepository;
    private readonly IRbacLookupCacheService _lookupCacheService;
    private readonly ICurrentTenant _currentTenant;
    private readonly IOptionsMonitor<XiHanOpenApiSecurityOptions> _optionsMonitor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacOpenApiSecurityClientStore(
        IOAuthAppRepository oauthAppRepository,
        IConfigRepository configRepository,
        IRbacLookupCacheService lookupCacheService,
        ICurrentTenant currentTenant,
        IOptionsMonitor<XiHanOpenApiSecurityOptions> optionsMonitor)
    {
        _oauthAppRepository = oauthAppRepository;
        _configRepository = configRepository;
        _lookupCacheService = lookupCacheService;
        _currentTenant = currentTenant;
        _optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc />
    public async Task<OpenApiSecurityClient?> FindByAccessKeyAsync(string accessKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
        {
            return null;
        }

        var normalizedAccessKey = accessKey.Trim();
        var options = _optionsMonitor.CurrentValue;
        var configuredClient = options.Clients.FirstOrDefault(item =>
            string.Equals(item.AccessKey, normalizedAccessKey, StringComparison.OrdinalIgnoreCase));

        var oauthClient = await FindOAuthClientAsync(normalizedAccessKey, _currentTenant.Id, cancellationToken);
        var persistedConfig = oauthClient is null
            ? null
            : await LoadOpenApiConfigAsync(oauthClient.ClientId, oauthClient.TenantId, cancellationToken);
        if (oauthClient is null && configuredClient is null)
        {
            return null;
        }

        if (oauthClient is not null && oauthClient.Status != YesOrNo.Yes)
        {
            return null;
        }

        if (configuredClient is not null && !configuredClient.IsEnabled)
        {
            return null;
        }

        var effectiveSecretKey = oauthClient?.ClientSecret ?? configuredClient?.SecretKey;
        if (string.IsNullOrWhiteSpace(effectiveSecretKey))
        {
            return null;
        }

        var mergedConfig = OpenApiClientSecurityConfigHelper.Normalize(
            new OpenApiClientSecurityConfig
            {
                IsEnabled = persistedConfig?.IsEnabled ?? true,
                SignatureAlgorithm = FirstNonEmpty(
                    persistedConfig?.SignatureAlgorithm,
                    configuredClient?.SignatureAlgorithm,
                    options.DefaultSignatureAlgorithm,
                    "HMACSHA256") ?? "HMACSHA256",
                ContentSignatureAlgorithm = FirstNonEmpty(
                    persistedConfig?.ContentSignatureAlgorithm,
                    configuredClient?.ContentSignatureAlgorithm,
                    options.DefaultContentSignatureAlgorithm,
                    "SHA256") ?? "SHA256",
                EncryptionAlgorithm = FirstNonEmpty(
                    persistedConfig?.EncryptionAlgorithm,
                    configuredClient?.EncryptionAlgorithm,
                    options.DefaultEncryptionAlgorithm,
                    "AES-CBC") ?? "AES-CBC",
                EncryptKey = FirstNonEmpty(
                    persistedConfig?.EncryptKey,
                    configuredClient?.EncryptKey,
                    effectiveSecretKey),
                PublicKey = FirstNonEmpty(
                    persistedConfig?.PublicKey,
                    configuredClient?.PublicKey),
                Sm2PublicKey = FirstNonEmpty(
                    persistedConfig?.Sm2PublicKey,
                    configuredClient?.Sm2PublicKey),
                AllowResponseEncryption = persistedConfig?.AllowResponseEncryption
                    ?? configuredClient?.AllowResponseEncryption
                    ?? true,
                IpWhitelist = string.IsNullOrWhiteSpace(persistedConfig?.IpWhitelist)
                    ? JoinIpWhitelist(configuredClient?.IpWhitelist)
                    : persistedConfig?.IpWhitelist
            },
            effectiveSecretKey);

        if (!mergedConfig.IsEnabled)
        {
            return null;
        }

        var ipWhitelist = OpenApiClientSecurityConfigHelper.ParseIpWhitelist(mergedConfig.IpWhitelist);

        return new OpenApiSecurityClient
        {
            AccessKey = oauthClient?.ClientId ?? configuredClient!.AccessKey,
            SecretKey = effectiveSecretKey,
            EncryptKey = mergedConfig.EncryptKey ?? effectiveSecretKey,
            PublicKey = mergedConfig.PublicKey,
            Sm2PublicKey = mergedConfig.Sm2PublicKey,
            SignatureAlgorithm = mergedConfig.SignatureAlgorithm,
            ContentSignatureAlgorithm = mergedConfig.ContentSignatureAlgorithm,
            EncryptionAlgorithm = mergedConfig.EncryptionAlgorithm,
            AllowResponseEncryption = mergedConfig.AllowResponseEncryption,
            IpWhitelist = [.. ipWhitelist],
            IsEnabled = true
        };
    }

    private async Task<OAuthAppDto?> FindOAuthClientAsync(
        string accessKey,
        long? tenantId,
        CancellationToken cancellationToken)
    {
        var currentTenantClient = await GetCachedOAuthClientAsync(accessKey, tenantId, cancellationToken);
        if (currentTenantClient is not null || !tenantId.HasValue)
        {
            return currentTenantClient;
        }

        // 允许租户请求回退到 host 侧 OAuth 客户端
        return await GetCachedOAuthClientAsync(accessKey, null, cancellationToken);
    }

    private Task<OAuthAppDto?> GetCachedOAuthClientAsync(
        string accessKey,
        long? tenantId,
        CancellationToken cancellationToken)
    {
        return _lookupCacheService.GetOAuthAppByClientIdAsync(
            accessKey,
            tenantId,
            async token =>
            {
                var entity = await _oauthAppRepository.GetByClientIdAsync(accessKey, tenantId, token);
                if (entity is null)
                {
                    return null;
                }

                return new OAuthAppDto
                {
                    ClientId = entity.ClientId,
                    ClientSecret = entity.ClientSecret,
                    TenantId = entity.TenantId,
                    Status = entity.Status
                };
            },
            cancellationToken);
    }

    private async Task<OpenApiClientSecurityConfig?> LoadOpenApiConfigAsync(
        string clientId,
        long? tenantId,
        CancellationToken cancellationToken)
    {
        var configKey = OpenApiClientSecurityConfigHelper.BuildConfigKey(clientId);
        var config = await _configRepository.GetByConfigKeyAsync(configKey, tenantId, cancellationToken);
        return OpenApiClientSecurityConfigHelper.Deserialize(config?.ConfigValue);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? JoinIpWhitelist(IReadOnlyCollection<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        return string.Join(",", values.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()));
    }
}
