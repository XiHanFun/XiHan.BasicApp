#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthTokenQueryService
// Guid:c14395e5-1957-4a40-aa0f-792349588312
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// OAuth Token 查询应用服务接口
/// </summary>
public interface IOAuthTokenQueryService : IApplicationService
{
    /// <summary>
    /// 获取 OAuth Token 分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth Token 分页列表</returns>
    Task<PageResultDtoBase<OAuthTokenListItemDto>> GetOAuthTokenPageAsync(OAuthTokenPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 OAuth Token 详情
    /// </summary>
    /// <param name="id">OAuth Token 主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth Token 详情</returns>
    Task<OAuthTokenDetailDto?> GetOAuthTokenDetailAsync(long id, CancellationToken cancellationToken = default);
}
