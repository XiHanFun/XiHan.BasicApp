#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppService
// Guid:07b77ac0-53ce-4db8-a1de-3535d84651a6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Security;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// OAuth应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class OAuthAppService
    : CrudApplicationServiceBase<SysOAuthApp, OAuthAppDto, long, OAuthAppCreateDto, OAuthAppUpdateDto, BasicAppPRDto>,
        IOAuthAppService
{
    private readonly IOAuthAppRepository _oauthAppRepository;
    private readonly IConfigRepository _configRepository;
    private readonly IOAuthAppQueryService _queryService;
    private readonly IOAuthAppDomainService _domainService;
    private readonly IRbacLookupCacheService _lookupCacheService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppService(
        IOAuthAppRepository oauthAppRepository,
        IConfigRepository configRepository,
        IOAuthAppQueryService queryService,
        IOAuthAppDomainService domainService,
        IRbacLookupCacheService lookupCacheService)
        : base(oauthAppRepository)
    {
        _oauthAppRepository = oauthAppRepository;
        _configRepository = configRepository;
        _queryService = queryService;
        _domainService = domainService;
        _lookupCacheService = lookupCacheService;
    }

    /// <summary>
    /// ID 查询（委托 QueryService，走缓存）
    /// </summary>
    public override async Task<OAuthAppDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据客户端ID获取应用
    /// </summary>
    public async Task<OAuthAppDto?> GetByClientIdAsync(string clientId, long? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        var normalizedClientId = clientId.Trim();

        return await _lookupCacheService.GetOAuthAppByClientIdAsync(
            normalizedClientId,
            tenantId,
            async token =>
            {
                var entity = await _oauthAppRepository.GetByClientIdAsync(normalizedClientId, tenantId, token);
                return entity?.Adapt<OAuthAppDto>();
            });
    }

    /// <summary>
    /// 获取 OpenAPI 安全配置
    /// </summary>
    public async Task<OAuthAppOpenApiSecurityDto> GetOpenApiSecurityAsync(long appId)
    {
        if (appId <= 0)
        {
            throw new ArgumentException("OAuth 应用 ID 无效", nameof(appId));
        }

        var app = await _oauthAppRepository.GetByIdAsync(appId)
                  ?? throw new KeyNotFoundException($"未找到 OAuth 应用: {appId}");
        var config = await LoadOpenApiConfigAsync(app.ClientId, app.TenantId);
        var normalized = OpenApiClientSecurityConfigHelper.Normalize(config, app.ClientSecret);

        return ToOpenApiSecurityDto(app.BasicId, normalized);
    }

    /// <summary>
    /// 更新 OpenAPI 安全配置
    /// </summary>
    public async Task<OAuthAppOpenApiSecurityDto> UpdateOpenApiSecurityAsync(OAuthAppOpenApiSecurityUpdateDto input)
    {
        input.ValidateAnnotations();
        var app = await _oauthAppRepository.GetByIdAsync(input.BasicId)
                  ?? throw new KeyNotFoundException($"未找到 OAuth 应用: {input.BasicId}");

        var normalized = OpenApiClientSecurityConfigHelper.Normalize(
            new OpenApiClientSecurityConfig
            {
                IsEnabled = input.IsEnabled,
                SignatureAlgorithm = input.SignatureAlgorithm,
                ContentSignatureAlgorithm = input.ContentSignatureAlgorithm,
                EncryptionAlgorithm = input.EncryptionAlgorithm,
                EncryptKey = input.EncryptKey,
                PublicKey = input.PublicKey,
                Sm2PublicKey = input.Sm2PublicKey,
                AllowResponseEncryption = input.AllowResponseEncryption,
                IpWhitelist = input.IpWhitelist
            },
            app.ClientSecret);

        var configKey = OpenApiClientSecurityConfigHelper.BuildConfigKey(app.ClientId);
        var existing = await _configRepository.GetByConfigKeyAsync(configKey, app.TenantId);
        if (existing is null)
        {
            await _configRepository.AddAsync(new SysConfig
            {
                TenantId = app.TenantId,
                ConfigName = OpenApiClientSecurityConfigHelper.BuildConfigName(app.ClientId),
                ConfigGroup = OpenApiClientSecurityConfigHelper.ConfigGroup,
                ConfigKey = configKey,
                ConfigValue = OpenApiClientSecurityConfigHelper.Serialize(normalized),
                ConfigType = ConfigType.Application,
                DataType = ConfigDataType.Json,
                ConfigDescription = OpenApiClientSecurityConfigHelper.BuildConfigDescription(app.ClientId),
                IsBuiltIn = false,
                IsEncrypted = false,
                Status = normalized.IsEnabled ? YesOrNo.Yes : YesOrNo.No
            });
        }
        else
        {
            existing.ConfigName = OpenApiClientSecurityConfigHelper.BuildConfigName(app.ClientId);
            existing.ConfigGroup = OpenApiClientSecurityConfigHelper.ConfigGroup;
            existing.ConfigValue = OpenApiClientSecurityConfigHelper.Serialize(normalized);
            existing.ConfigType = ConfigType.Application;
            existing.DataType = ConfigDataType.Json;
            existing.ConfigDescription = OpenApiClientSecurityConfigHelper.BuildConfigDescription(app.ClientId);
            existing.Status = normalized.IsEnabled ? YesOrNo.Yes : YesOrNo.No;
            await _configRepository.UpdateAsync(existing);
        }

        await _lookupCacheService.InvalidateOAuthAppLookupAsync(app.TenantId);
        return ToOpenApiSecurityDto(app.BasicId, normalized);
    }

    /// <summary>
    /// 创建 OAuth 应用（委托 DomainService）
    /// </summary>
    public override async Task<OAuthAppDto> CreateAsync(OAuthAppCreateDto input)
    {
        input.ValidateAnnotations();
        var entity = await MapDtoToEntityAsync(input);
        var created = await _domainService.CreateAsync(entity);
        return created.Adapt<OAuthAppDto>()!;
    }

    /// <summary>
    /// 更新 OAuth 应用（委托 DomainService）
    /// </summary>
    public override async Task<OAuthAppDto> UpdateAsync(OAuthAppUpdateDto input)
    {
        input.ValidateAnnotations();
        var entity = await Repository.GetByIdAsync(input.BasicId)
                     ?? throw new KeyNotFoundException($"未找到 OAuth 应用: {input.BasicId}");
        await MapDtoToEntityAsync(input, entity);
        var updated = await _domainService.UpdateAsync(entity);
        return updated.Adapt<OAuthAppDto>()!;
    }

    /// <summary>
    /// 删除 OAuth 应用（委托 DomainService）
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        return await _domainService.DeleteAsync(id);
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    protected override Task<SysOAuthApp> MapDtoToEntityAsync(OAuthAppCreateDto createDto)
    {
        var entity = new SysOAuthApp
        {
            TenantId = createDto.TenantId ?? 0,
            AppName = createDto.AppName.Trim(),
            AppDescription = createDto.AppDescription,
            ClientId = createDto.ClientId.Trim(),
            ClientSecret = createDto.ClientSecret.Trim(),
            AppType = createDto.AppType,
            GrantTypes = createDto.GrantTypes.Trim(),
            RedirectUris = createDto.RedirectUris,
            Scopes = createDto.Scopes,
            AccessTokenLifetime = createDto.AccessTokenLifetime,
            RefreshTokenLifetime = createDto.RefreshTokenLifetime,
            AuthorizationCodeLifetime = createDto.AuthorizationCodeLifetime,
            SkipConsent = createDto.SkipConsent,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    protected override Task MapDtoToEntityAsync(OAuthAppUpdateDto updateDto, SysOAuthApp entity)
    {
        entity.AppName = updateDto.AppName.Trim();
        entity.AppDescription = updateDto.AppDescription;
        entity.ClientSecret = updateDto.ClientSecret.Trim();
        entity.AppType = updateDto.AppType;
        entity.GrantTypes = updateDto.GrantTypes.Trim();
        entity.RedirectUris = updateDto.RedirectUris;
        entity.Scopes = updateDto.Scopes;
        entity.AccessTokenLifetime = updateDto.AccessTokenLifetime;
        entity.RefreshTokenLifetime = updateDto.RefreshTokenLifetime;
        entity.AuthorizationCodeLifetime = updateDto.AuthorizationCodeLifetime;
        entity.SkipConsent = updateDto.SkipConsent;
        entity.Status = updateDto.Status;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }

    private static OAuthAppOpenApiSecurityDto ToOpenApiSecurityDto(long appId, OpenApiClientSecurityConfig config)
    {
        return new OAuthAppOpenApiSecurityDto
        {
            BasicId = appId,
            IsEnabled = config.IsEnabled,
            SignatureAlgorithm = config.SignatureAlgorithm,
            ContentSignatureAlgorithm = config.ContentSignatureAlgorithm,
            EncryptionAlgorithm = config.EncryptionAlgorithm,
            EncryptKey = config.EncryptKey,
            PublicKey = config.PublicKey,
            Sm2PublicKey = config.Sm2PublicKey,
            AllowResponseEncryption = config.AllowResponseEncryption,
            IpWhitelist = config.IpWhitelist
        };
    }

    private async Task<OpenApiClientSecurityConfig?> LoadOpenApiConfigAsync(string clientId, long? tenantId)
    {
        var configKey = OpenApiClientSecurityConfigHelper.BuildConfigKey(clientId);
        var config = await _configRepository.GetByConfigKeyAsync(configKey, tenantId);
        return OpenApiClientSecurityConfigHelper.Deserialize(config?.ConfigValue);
    }
}
