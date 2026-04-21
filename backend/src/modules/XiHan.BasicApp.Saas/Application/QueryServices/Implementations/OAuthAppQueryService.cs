#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppQueryService
// Guid:6a7b8c9d-0e1f-4345-f012-620000000002
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// OAuth 应用查询服务
/// </summary>
public class OAuthAppQueryService : IOAuthAppQueryService, ITransientDependency
{
    private readonly IOAuthAppRepository _oauthAppRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppQueryService(IOAuthAppRepository oauthAppRepository)
    {
        _oauthAppRepository = oauthAppRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = QueryCacheKeys.OAuthAppById, ExpireSeconds = 300)]
    public async Task<OAuthAppDto?> GetByIdAsync(long id)
    {
        var entity = await _oauthAppRepository.GetByIdAsync(id);
        return entity?.Adapt<OAuthAppDto>();
    }
}
