#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthTokenQueryService
// Guid:d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// OAuth令牌查询服务（处理OAuth令牌的读操作 - CQRS）
/// </summary>
public class OAuthTokenQueryService : ApplicationServiceBase
{
    private readonly IOAuthTokenRepository _oAuthTokenRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthTokenQueryService(IOAuthTokenRepository oAuthTokenRepository)
    {
        _oAuthTokenRepository = oAuthTokenRepository;
    }

    /// <summary>
    /// 根据访问令牌获取令牌信息
    /// </summary>
    public async Task<RbacDtoBase?> GetByAccessTokenAsync(string accessToken)
    {
        var token = await _oAuthTokenRepository.GetByAccessTokenAsync(accessToken);
        return token?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据刷新令牌获取令牌信息
    /// </summary>
    public async Task<RbacDtoBase?> GetByRefreshTokenAsync(string refreshToken)
    {
        var token = await _oAuthTokenRepository.GetByRefreshTokenAsync(refreshToken);
        return token?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取用户的有效令牌列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetValidTokensByUserIdAsync(long userId)
    {
        var tokens = await _oAuthTokenRepository.GetValidTokensByUserIdAsync(userId);
        return tokens.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 验证访问令牌
    /// </summary>
    public async Task<bool> ValidateAccessTokenAsync(string accessToken)
    {
        return await _oAuthTokenRepository.ValidateAccessTokenAsync(accessToken);
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _oAuthTokenRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
