#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewQueryService
// Guid:7f809102-1324-3456-f012-3456789abc02
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
/// 审核查询服务
/// </summary>
public class ReviewQueryService : IReviewQueryService, ITransientDependency
{
    private readonly IReviewRepository _reviewRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewQueryService(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = QueryCacheKeys.ReviewById, ExpireSeconds = 300)]
    public async Task<ReviewDto?> GetByIdAsync(long id)
    {
        var entity = await _reviewRepository.GetByIdAsync(id);
        return entity?.Adapt<ReviewDto>();
    }
}
