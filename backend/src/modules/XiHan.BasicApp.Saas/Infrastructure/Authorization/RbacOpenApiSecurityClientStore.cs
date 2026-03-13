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
    private readonly IRbacLookupCacheService _lookupCacheService;
    private readonly ICurrentTenant _currentTenant;
    private readonly IOptionsMonitor<XiHanOpenApiSecurityOptions> _optionsMonitor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacOpenApiSecurityClientStore(
        IOAuthAppRepository oauthAppRepository,
        IRbacLookupCacheService lookupCacheService,
        ICurrentTenant currentTenant,
        IOptionsMonitor<XiHanOpenApiSecurityOptions> optionsMonitor)
    {
        _oauthAppRepository = oauthAppRepository;
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

        if (oauthClient is null)
        {
            return new OpenApiSecurityClient
            {
                AccessKey = configuredClient!.AccessKey,
                SecretKey = configuredClient.SecretKey,
                EncryptKey = configuredClient.EncryptKey,
                PublicKey = configuredClient.PublicKey,
                Sm2PublicKey = configuredClient.Sm2PublicKey,
                SignatureAlgorithm = configuredClient.SignatureAlgorithm,
                ContentSignatureAlgorithm = configuredClient.ContentSignatureAlgorithm,
                EncryptionAlgorithm = configuredClient.EncryptionAlgorithm,
                AllowResponseEncryption = configuredClient.AllowResponseEncryption,
                IpWhitelist = [.. configuredClient.IpWhitelist],
                IsEnabled = true
            };
        }

        var mergedEncryptKey = string.IsNullOrWhiteSpace(configuredClient?.EncryptKey)
            ? oauthClient.ClientSecret
            : configuredClient.EncryptKey;

        return new OpenApiSecurityClient
        {
            AccessKey = oauthClient.ClientId,
            SecretKey = oauthClient.ClientSecret,
            EncryptKey = mergedEncryptKey,
            PublicKey = configuredClient?.PublicKey,
            Sm2PublicKey = configuredClient?.Sm2PublicKey,
            SignatureAlgorithm = configuredClient?.SignatureAlgorithm,
            ContentSignatureAlgorithm = configuredClient?.ContentSignatureAlgorithm,
            EncryptionAlgorithm = configuredClient?.EncryptionAlgorithm,
            AllowResponseEncryption = configuredClient?.AllowResponseEncryption ?? true,
            IpWhitelist = configuredClient is null ? [] : [.. configuredClient.IpWhitelist],
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
                    Status = entity.Status
                };
            },
            cancellationToken);
    }
}
