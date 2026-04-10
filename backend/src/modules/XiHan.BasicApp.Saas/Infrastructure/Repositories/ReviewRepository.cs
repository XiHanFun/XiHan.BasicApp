#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewRepository
// Guid:58d5e397-71f4-40fa-bb58-f11c95800742
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:21:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 审查仓储实现
/// </summary>
public class ReviewRepository : SqlSugarAggregateRepository<SysReview, long>, IReviewRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据审查编码获取审查
    /// </summary>
    public async Task<SysReview?> GetByReviewCodeAsync(string reviewCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewCode);
        var resolvedTenantId = tenantId;

        var query = CreateTenantQueryable()
            .Where(review => review.ReviewCode == reviewCode);

        query = resolvedTenantId.HasValue
            ? query.Where(review => review.TenantId == resolvedTenantId.Value)
            : query.Where(review => review.TenantId == null);

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 校验审查编码是否已存在
    /// </summary>
    public async Task<bool> IsReviewCodeExistsAsync(string reviewCode, long? tenantId = null, long? excludeReviewId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewCode);
        var resolvedTenantId = tenantId;

        var query = CreateTenantQueryable()
            .Where(review => review.ReviewCode == reviewCode);

        query = resolvedTenantId.HasValue
            ? query.Where(review => review.TenantId == resolvedTenantId.Value)
            : query.Where(review => review.TenantId == null);

        if (excludeReviewId.HasValue)
        {
            query = query.Where(review => review.BasicId != excludeReviewId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
