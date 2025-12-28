#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthTokenService
// Guid:k1l2m3n4-o5p6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 19:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.OAuthTokens;
using XiHan.BasicApp.Rbac.Services.OAuthTokens.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.OAuthTokens;

/// <summary>
/// 系统OAuth令牌服务实现
/// </summary>
public class SysOAuthTokenService : CrudApplicationServiceBase<SysOAuthToken, OAuthTokenDto, XiHanBasicAppIdType, CreateOAuthTokenDto, UpdateOAuthTokenDto>, ISysOAuthTokenService
{
    private readonly ISysOAuthTokenRepository _oauthTokenRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysOAuthTokenService(ISysOAuthTokenRepository oauthTokenRepository) : base(oauthTokenRepository)
    {
        _oauthTokenRepository = oauthTokenRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据访问令牌获取
    /// </summary>
    public async Task<OAuthTokenDto?> GetByAccessTokenAsync(string accessToken)
    {
        var token = await _oauthTokenRepository.GetByAccessTokenAsync(accessToken);
        return token?.ToDto();
    }

    /// <summary>
    /// 根据刷新令牌获取
    /// </summary>
    public async Task<OAuthTokenDto?> GetByRefreshTokenAsync(string refreshToken)
    {
        var token = await _oauthTokenRepository.GetByRefreshTokenAsync(refreshToken);
        return token?.ToDto();
    }

    /// <summary>
    /// 根据客户端ID和用户ID获取令牌列表
    /// </summary>
    public async Task<List<OAuthTokenDto>> GetByClientAndUserAsync(string clientId, XiHanBasicAppIdType userId)
    {
        var tokens = await _oauthTokenRepository.GetByClientAndUserAsync(clientId, userId);
        return tokens.ToDto();
    }

    /// <summary>
    /// 删除过期的令牌
    /// </summary>
    public async Task<int> DeleteExpiredTokensAsync()
    {
        return await _oauthTokenRepository.DeleteExpiredTokensAsync();
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<OAuthTokenDto> MapToEntityDtoAsync(SysOAuthToken entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 OAuthTokenDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysOAuthToken> MapToEntityAsync(OAuthTokenDto dto)
    {
        var entity = new SysOAuthToken
        {
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            TokenType = dto.TokenType,
            ClientId = dto.ClientId,
            UserId = dto.UserId,
            GrantType = dto.GrantType,
            Scopes = dto.Scopes,
            AccessTokenExpiresAt = dto.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = dto.RefreshTokenExpiresAt,
            IsRevoked = dto.IsRevoked,
            RevokedAt = dto.RevokedAt
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 OAuthTokenDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(OAuthTokenDto dto, SysOAuthToken entity)
    {
        entity.AccessToken = dto.AccessToken;
        entity.RefreshToken = dto.RefreshToken;
        entity.TokenType = dto.TokenType;
        entity.ClientId = dto.ClientId;
        entity.UserId = dto.UserId;
        entity.GrantType = dto.GrantType;
        entity.Scopes = dto.Scopes;
        entity.AccessTokenExpiresAt = dto.AccessTokenExpiresAt;
        entity.RefreshTokenExpiresAt = dto.RefreshTokenExpiresAt;
        entity.IsRevoked = dto.IsRevoked;
        entity.RevokedAt = dto.RevokedAt;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysOAuthToken> MapToEntityAsync(CreateOAuthTokenDto createDto)
    {
        var entity = new SysOAuthToken
        {
            AccessToken = createDto.AccessToken,
            RefreshToken = createDto.RefreshToken,
            TokenType = createDto.TokenType,
            ClientId = createDto.ClientId,
            UserId = createDto.UserId,
            GrantType = createDto.GrantType,
            Scopes = createDto.Scopes,
            AccessTokenExpiresAt = createDto.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = createDto.RefreshTokenExpiresAt
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateOAuthTokenDto updateDto, SysOAuthToken entity)
    {
        if (updateDto.IsRevoked.HasValue) entity.IsRevoked = updateDto.IsRevoked.Value;
        if (updateDto.RevokedAt.HasValue) entity.RevokedAt = updateDto.RevokedAt;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}

