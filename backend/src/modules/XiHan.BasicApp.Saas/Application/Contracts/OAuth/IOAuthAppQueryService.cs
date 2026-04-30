#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthAppQueryService
// Guid:6a484720-ad66-4dfa-9be1-00fe8081660a
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
/// OAuth 应用查询应用服务接口
/// </summary>
public interface IOAuthAppQueryService : IApplicationService
{
    /// <summary>
    /// 获取 OAuth 应用分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth 应用分页列表</returns>
    Task<PageResultDtoBase<OAuthAppListItemDto>> GetOAuthAppPageAsync(OAuthAppPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 OAuth 应用详情
    /// </summary>
    /// <param name="id">OAuth 应用主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth 应用详情</returns>
    Task<OAuthAppDetailDto?> GetOAuthAppDetailAsync(long id, CancellationToken cancellationToken = default);
}
