#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthTokenCommandService
// Guid:c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// OAuth令牌命令服务（处理OAuth令牌的写操作）
/// </summary>
public class OAuthTokenCommandService : CrudApplicationServiceBase<SysOAuthToken, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IOAuthTokenRepository _oAuthTokenRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthTokenCommandService(IOAuthTokenRepository oAuthTokenRepository)
        : base(oAuthTokenRepository)
    {
        _oAuthTokenRepository = oAuthTokenRepository;
    }

    /// <summary>
    /// 创建OAuth令牌（重写以生成Token）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 映射实体
        var token = input.Adapt<SysOAuthToken>();

        // 2. 生成访问令牌和刷新令牌
        token.AccessToken = GenerateAccessToken();
        token.RefreshToken = GenerateRefreshToken();

        // 3. 设置过期时间
        token.AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(2); // 2小时后过期
        token.RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30); // 30天后过期

        // 4. 保存
        token = await _oAuthTokenRepository.AddAsync(token);

        return await MapToEntityDtoAsync(token);
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>新的令牌DTO</returns>
    public async Task<RbacDtoBase> RefreshTokenAsync(string refreshToken)
    {
        // 1. 获取旧令牌
        var oldToken = await _oAuthTokenRepository.GetByRefreshTokenAsync(refreshToken);
        if (oldToken == null)
        {
            throw new InvalidOperationException("刷新令牌不存在");
        }

        // 2. 检查刷新令牌是否过期
        if (oldToken.RefreshTokenExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("刷新令牌已过期");
        }

        // 3. 检查是否已被撤销
        if (oldToken.IsRevoked)
        {
            throw new InvalidOperationException("令牌已被撤销");
        }

        // 4. 生成新令牌
        var newToken = new SysOAuthToken
        {
            UserId = oldToken.UserId,
            ClientId = oldToken.ClientId,
            AccessToken = GenerateAccessToken(),
            RefreshToken = GenerateRefreshToken(),
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(2),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            Scope = oldToken.Scope
        };

        // 5. 撤销旧令牌
        oldToken.IsRevoked = true;
        await _oAuthTokenRepository.UpdateAsync(oldToken);

        // 6. 保存新令牌
        newToken = await _oAuthTokenRepository.AddAsync(newToken);

        return await MapToEntityDtoAsync(newToken);
    }

    /// <summary>
    /// 撤销令牌
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    public async Task<bool> RevokeTokenAsync(string accessToken)
    {
        return await _oAuthTokenRepository.RevokeTokenAsync(accessToken);
    }

    /// <summary>
    /// 清理过期令牌
    /// </summary>
    public async Task<int> CleanupExpiredTokensAsync()
    {
        return await _oAuthTokenRepository.CleanupExpiredTokensAsync();
    }

    /// <summary>
    /// 生成访问令牌
    /// </summary>
    private static string GenerateAccessToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray() + Guid.NewGuid().ToByteArray());
    }

    /// <summary>
    /// 生成刷新令牌
    /// </summary>
    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray() + Guid.NewGuid().ToByteArray() + Guid.NewGuid().ToByteArray());
    }
}
