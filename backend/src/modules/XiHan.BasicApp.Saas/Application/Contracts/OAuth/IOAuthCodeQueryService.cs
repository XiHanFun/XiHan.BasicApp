// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// OAuth 授权码查询应用服务接口
/// </summary>
public interface IOAuthCodeQueryService : IApplicationService
{
    /// <summary>
    /// 获取 OAuth 授权码分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth 授权码分页列表</returns>
    Task<PageResultDtoBase<OAuthCodeListItemDto>> GetOAuthCodePageAsync(OAuthCodePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 OAuth 授权码详情
    /// </summary>
    /// <param name="id">OAuth 授权码主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth 授权码详情</returns>
    Task<OAuthCodeDetailDto?> GetOAuthCodeDetailAsync(long id, CancellationToken cancellationToken = default);
}
