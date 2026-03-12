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
using XiHan.BasicApp.Saas.Domain.Entities;
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
    private readonly IRbacLookupCacheService _lookupCacheService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppService(IOAuthAppRepository oauthAppRepository, IRbacLookupCacheService lookupCacheService)
        : base(oauthAppRepository)
    {
        _oauthAppRepository = oauthAppRepository;
        _lookupCacheService = lookupCacheService;
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
    /// 创建 OAuth 应用
    /// </summary>
    public override async Task<OAuthAppDto> CreateAsync(OAuthAppCreateDto input)
    {
        input.ValidateAnnotations();

        var normalizedClientId = input.ClientId.Trim();
        var exists = await _oauthAppRepository.IsClientIdExistsAsync(normalizedClientId, input.TenantId);
        if (exists)
        {
            throw new BusinessException(message: $"客户端ID '{normalizedClientId}' 已存在");
        }

        var dto = await base.CreateAsync(input);
        await _lookupCacheService.InvalidateOAuthAppLookupAsync(input.TenantId);
        return dto;
    }

    /// <summary>
    /// 更新 OAuth 应用
    /// </summary>
    public override async Task<OAuthAppDto> UpdateAsync(OAuthAppUpdateDto input)
    {
        input.ValidateAnnotations();
        var entity = await _oauthAppRepository.GetByIdAsync(input.BasicId)
                     ?? throw new KeyNotFoundException($"未找到 OAuth 应用: {input.BasicId}");
        var dto = await base.UpdateAsync(input);
        await _lookupCacheService.InvalidateOAuthAppLookupAsync(entity.TenantId);
        return dto;
    }

    /// <summary>
    /// 删除 OAuth 应用
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public override async Task<bool> DeleteAsync(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("OAuth 应用 ID 无效", nameof(id));
        }

        var entity = await _oauthAppRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return false;
        }

        var deleted = await base.DeleteAsync(id);
        if (deleted)
        {
            await _lookupCacheService.InvalidateOAuthAppLookupAsync(entity.TenantId);
        }

        return deleted;
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    protected override Task<SysOAuthApp> MapDtoToEntityAsync(OAuthAppCreateDto createDto)
    {
        var entity = new SysOAuthApp
        {
            TenantId = createDto.TenantId,
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
}
