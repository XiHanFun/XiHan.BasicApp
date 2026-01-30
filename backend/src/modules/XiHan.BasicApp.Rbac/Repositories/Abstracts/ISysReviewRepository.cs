#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysReviewRepository
// Guid:f8a9b0c1-d2e3-4567-8901-234567f23456
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 审批评审仓储接口
/// </summary>
/// <remarks>
/// 聚合范围：SysReview + SysReviewLog
/// </remarks>
public interface ISysReviewRepository : IAggregateRootRepository<SysReview, long>
{
    /// <summary>
    /// 获取待审批列表
    /// </summary>
    Task<List<SysReview>> GetPendingReviewsAsync(long reviewerId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的审批记录
    /// </summary>
    Task<List<SysReview>> GetByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取审批及日志
    /// </summary>
    Task<SysReview?> GetWithLogsAsync(long reviewId, CancellationToken cancellationToken = default);

    // ========== 审批日志 ==========

    /// <summary>
    /// 添加审批日志
    /// </summary>
    Task<SysReviewLog> AddReviewLogAsync(SysReviewLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取审批日志列表
    /// </summary>
    Task<List<SysReviewLog>> GetReviewLogsAsync(long reviewId, CancellationToken cancellationToken = default);
}
