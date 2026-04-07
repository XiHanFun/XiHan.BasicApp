#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewDomainService
// Guid:7f809102-1324-3456-f012-3456789abc04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 审核领域服务
/// </summary>
public class ReviewDomainService : IReviewDomainService, ITransientDependency
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewDomainService(IReviewRepository reviewRepository, ILocalEventBus localEventBus)
    {
        _reviewRepository = reviewRepository;
        _localEventBus = localEventBus;
    }

    /// <inheritdoc />
    public async Task<SysReview> CreateAsync(SysReview review)
    {
        var created = await _reviewRepository.AddAsync(review);
        await _localEventBus.PublishAsync(new ReviewChangedDomainEvent(created.BasicId));
        return created;
    }

    /// <inheritdoc />
    public async Task<SysReview> UpdateAsync(SysReview review)
    {
        var updated = await _reviewRepository.UpdateAsync(review);
        await _localEventBus.PublishAsync(new ReviewChangedDomainEvent(updated.BasicId));
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null) return false;

        var result = await _reviewRepository.DeleteAsync(review);
        if (result)
        {
            await _localEventBus.PublishAsync(new ReviewChangedDomainEvent(id));
        }
        return result;
    }
}
