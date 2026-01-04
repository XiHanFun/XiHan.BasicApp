#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthAppService
// Guid:e1f2g3h4-i5j6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.OAuthApps;
using XiHan.BasicApp.Rbac.Services.OAuthApps.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.OAuthApps;

/// <summary>
/// 系统OAuth应用服务实现
/// </summary>
public class SysOAuthAppService : CrudApplicationServiceBase<SysOAuthApp, OAuthAppDto, XiHanBasicAppIdType, CreateOAuthAppDto, UpdateOAuthAppDto>, ISysOAuthAppService
{
    private readonly ISysOAuthAppRepository _oauthAppRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysOAuthAppService(ISysOAuthAppRepository oauthAppRepository) : base(oauthAppRepository)
    {
        _oauthAppRepository = oauthAppRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据客户端ID获取应用
    /// </summary>
    public async Task<OAuthAppDto?> GetByClientIdAsync(string clientId)
    {
        var app = await _oauthAppRepository.GetByClientIdAsync(clientId);
        return app?.ToDto();
    }

    /// <summary>
    /// 根据应用名称获取应用
    /// </summary>
    public async Task<OAuthAppDto?> GetByAppNameAsync(string appName)
    {
        var app = await _oauthAppRepository.GetByAppNameAsync(appName);
        return app?.ToDto();
    }

    /// <summary>
    /// 检查客户端ID是否存在
    /// </summary>
    public async Task<bool> ExistsByClientIdAsync(string clientId, XiHanBasicAppIdType? excludeId = null)
    {
        return await _oauthAppRepository.ExistsByClientIdAsync(clientId, excludeId);
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<OAuthAppDto> MapToEntityDtoAsync(SysOAuthApp entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 OAuthAppDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysOAuthApp> MapToEntityAsync(OAuthAppDto dto)
    {
        var entity = new SysOAuthApp
        {
            AppName = dto.AppName,
            AppDescription = dto.AppDescription,
            ClientId = dto.ClientId,
            ClientSecret = dto.ClientSecret,
            AppType = dto.AppType,
            GrantTypes = dto.GrantTypes,
            RedirectUris = dto.RedirectUris,
            Scopes = dto.Scopes,
            AccessTokenLifetime = dto.AccessTokenLifetime,
            RefreshTokenLifetime = dto.RefreshTokenLifetime,
            AuthorizationCodeLifetime = dto.AuthorizationCodeLifetime,
            Logo = dto.Logo,
            Homepage = dto.Homepage,
            SkipConsent = dto.SkipConsent,
            Status = dto.Status,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 OAuthAppDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(OAuthAppDto dto, SysOAuthApp entity)
    {
        entity.AppName = dto.AppName;
        entity.AppDescription = dto.AppDescription;
        entity.ClientId = dto.ClientId;
        entity.ClientSecret = dto.ClientSecret;
        entity.AppType = dto.AppType;
        entity.GrantTypes = dto.GrantTypes;
        entity.RedirectUris = dto.RedirectUris;
        entity.Scopes = dto.Scopes;
        entity.AccessTokenLifetime = dto.AccessTokenLifetime;
        entity.RefreshTokenLifetime = dto.RefreshTokenLifetime;
        entity.AuthorizationCodeLifetime = dto.AuthorizationCodeLifetime;
        entity.Logo = dto.Logo;
        entity.Homepage = dto.Homepage;
        entity.SkipConsent = dto.SkipConsent;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysOAuthApp> MapToEntityAsync(CreateOAuthAppDto createDto)
    {
        var entity = new SysOAuthApp
        {
            AppName = createDto.AppName,
            AppDescription = createDto.AppDescription,
            ClientId = createDto.ClientId,
            ClientSecret = createDto.ClientSecret,
            AppType = createDto.AppType,
            GrantTypes = createDto.GrantTypes,
            RedirectUris = createDto.RedirectUris,
            Scopes = createDto.Scopes,
            AccessTokenLifetime = createDto.AccessTokenLifetime,
            RefreshTokenLifetime = createDto.RefreshTokenLifetime,
            AuthorizationCodeLifetime = createDto.AuthorizationCodeLifetime,
            Logo = createDto.Logo,
            Homepage = createDto.Homepage,
            SkipConsent = createDto.SkipConsent,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateOAuthAppDto updateDto, SysOAuthApp entity)
    {
        if (updateDto.AppName != null) entity.AppName = updateDto.AppName;
        if (updateDto.AppDescription != null) entity.AppDescription = updateDto.AppDescription;
        if (updateDto.ClientSecret != null) entity.ClientSecret = updateDto.ClientSecret;
        if (updateDto.AppType.HasValue) entity.AppType = updateDto.AppType.Value;
        if (updateDto.GrantTypes != null) entity.GrantTypes = updateDto.GrantTypes;
        if (updateDto.RedirectUris != null) entity.RedirectUris = updateDto.RedirectUris;
        if (updateDto.Scopes != null) entity.Scopes = updateDto.Scopes;
        if (updateDto.AccessTokenLifetime.HasValue) entity.AccessTokenLifetime = updateDto.AccessTokenLifetime.Value;
        if (updateDto.RefreshTokenLifetime.HasValue) entity.RefreshTokenLifetime = updateDto.RefreshTokenLifetime.Value;
        if (updateDto.AuthorizationCodeLifetime.HasValue) entity.AuthorizationCodeLifetime = updateDto.AuthorizationCodeLifetime.Value;
        if (updateDto.Logo != null) entity.Logo = updateDto.Logo;
        if (updateDto.Homepage != null) entity.Homepage = updateDto.Homepage;
        if (updateDto.SkipConsent.HasValue) entity.SkipConsent = updateDto.SkipConsent.Value;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
