#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewRepository
// Guid:a828152c-d6e9-4396-addb-b479254bad90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 审查仓储实现
/// </summary>
public class ReviewRepository : SqlSugarAggregateRepository<SysReview, long>, IReviewRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据审查编码查询审查
    /// </summary>
    public async Task<SysReview?> GetByReviewCodeAsync(string reviewCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReview>()
            .FirstAsync(r => r.ReviewCode == reviewCode, cancellationToken);
    }

    /// <summary>
    /// 检查审查编码是否存在
    /// </summary>
    public async Task<bool> ExistsByReviewCodeAsync(string reviewCode, long? excludeReviewId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysReview>()
            .Where(r => r.ReviewCode == reviewCode);

        if (excludeReviewId.HasValue)
        {
            query = query.Where(r => r.BasicId != excludeReviewId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 根据审查状态获取审查列表
    /// </summary>
    public async Task<List<SysReview>> GetByReviewStatusAsync(AuditStatus reviewStatus, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReview>()
            .Where(r => r.ReviewStatus == reviewStatus)
            .OrderBy(r => r.Priority, OrderByType.Asc)
            .OrderBy(r => r.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据审查类型获取审查列表
    /// </summary>
    public async Task<List<SysReview>> GetByReviewTypeAsync(string reviewType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReview>()
            .Where(r => r.ReviewType == reviewType)
            .OrderBy(r => r.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据提交人ID获取审查列表
    /// </summary>
    public async Task<List<SysReview>> GetBySubmitterIdAsync(long submitterId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReview>()
            .Where(r => r.SubmitterId == submitterId)
            .OrderBy(r => r.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据当前审查人ID获取审查列表
    /// </summary>
    public async Task<List<SysReview>> GetByCurrentReviewerIdAsync(long reviewerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReview>()
            .Where(r => r.CurrentReviewerId == reviewerId)
            .OrderBy(r => r.Priority, OrderByType.Asc)
            .OrderBy(r => r.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据业务实体获取审查列表
    /// </summary>
    public async Task<List<SysReview>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReview>()
            .Where(r => r.EntityType == entityType && r.EntityId == entityId)
            .OrderBy(r => r.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取待审查列表
    /// </summary>
    public async Task<List<SysReview>> GetPendingReviewsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReview>()
            .Where(r => r.ReviewStatus == AuditStatus.Pending || r.ReviewStatus == AuditStatus.InProgress)
            .Where(r => r.Status == YesOrNo.Yes)
            .OrderBy(r => r.Priority, OrderByType.Asc)
            .OrderBy(r => r.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定时间段内的审查列表
    /// </summary>
    public async Task<List<SysReview>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReview>()
            .Where(r => r.CreatedTime >= startTime && r.CreatedTime <= endTime)
            .OrderBy(r => r.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 更新审查状态
    /// </summary>
    public async Task<bool> UpdateReviewStatusAsync(long reviewId, AuditStatus reviewStatus, AuditResult? reviewResult, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysReview>()
            .SetColumns(r => new SysReview
            {
                ReviewStatus = reviewStatus,
                ReviewResult = reviewResult
            })
            .Where(r => r.BasicId == reviewId)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <summary>
    /// 更新当前审查人
    /// </summary>
    public async Task<bool> UpdateCurrentReviewerAsync(long reviewId, long reviewerId, string reviewerName, int currentLevel, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysReview>()
            .SetColumns(r => new SysReview
            {
                CurrentReviewerId = reviewerId,
                CurrentReviewerName = reviewerName,
                CurrentLevel = currentLevel
            })
            .Where(r => r.BasicId == reviewId)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }
}
