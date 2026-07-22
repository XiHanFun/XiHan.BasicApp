// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 审查仓储实现
/// </summary>
public sealed class ReviewRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysReview>(clientResolver, unitOfWorkManager), IReviewRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysReview>> GetPendingByReviewerIdAsync(long reviewerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(review => review.CurrentReviewUserId == reviewerId && review.ReviewStatus == AuditStatus.Pending)
            .ToListAsync(cancellationToken);
    }
}
