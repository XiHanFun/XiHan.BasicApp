#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysReviewRepository
// Guid:f6a7b8c9-d0e1-2345-6789-012345f01234
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 审批评审仓储实现
/// </summary>
public class SysReviewRepository : SqlSugarAggregateRepository<SysReview, long>, ISysReviewRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysReviewRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取待审批列表
    /// </summary>
    public async Task<List<SysReview>> GetPendingReviewsAsync(long reviewerId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysReview>()
            .Where(r => r.CurrentReviewUserId == reviewerId && r.ReviewStatus == AuditStatus.Pending)
            .OrderByDescending(r => r.CreatedTime)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);
    }

    /// <summary>
    /// 获取用户的审批记录
    /// </summary>
    public async Task<List<SysReview>> GetByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysReview>()
            .Where(r => r.CurrentReviewUserId == userId)
            .OrderByDescending(r => r.CreatedTime)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);
    }

    /// <summary>
    /// 获取审批及日志
    /// </summary>
    public async Task<SysReview?> GetWithLogsAsync(long reviewId, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(reviewId, cancellationToken);
    }

    // ========== 审批日志 ==========

    /// <summary>
    /// 添加审批日志
    /// </summary>
    public async Task<SysReviewLog> AddReviewLogAsync(SysReviewLog log, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(log).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 获取审批日志列表
    /// </summary>
    public async Task<List<SysReviewLog>> GetReviewLogsAsync(long reviewId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysReviewLog>()
            .Where(log => log.ReviewId == reviewId)
            .OrderBy(log => log.ReviewTime)
            .ToListAsync(cancellationToken);
    }
}
