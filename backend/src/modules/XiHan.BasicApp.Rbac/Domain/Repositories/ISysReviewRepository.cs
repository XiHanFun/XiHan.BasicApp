#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysReviewRepository
// Guid:a7b8c9d0-e1f2-3456-789a-bcdef1234567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统审查仓储接口
/// </summary>
public interface ISysReviewRepository : IAggregateRootRepository<SysReview, long>
{
    /// <summary>
    /// 根据审查编码获取审查
    /// </summary>
    /// <param name="reviewCode">审查编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查实体</returns>
    Task<SysReview?> GetByReviewCodeAsync(string reviewCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户待审查列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查列表</returns>
    Task<List<SysReview>> GetPendingReviewsByUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户已审查列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查列表</returns>
    Task<List<SysReview>> GetCompletedReviewsByUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据业务实体获取审查
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查实体</returns>
    Task<SysReview?> GetByBusinessEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存审查
    /// </summary>
    /// <param name="review">审查实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的审查实体</returns>
    Task<SysReview> SaveAsync(SysReview review, CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交审查
    /// </summary>
    /// <param name="reviewId">审查ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SubmitReviewAsync(long reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批准审查
    /// </summary>
    /// <param name="reviewId">审查ID</param>
    /// <param name="reviewerId">审查人ID</param>
    /// <param name="comment">审查意见</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ApproveReviewAsync(long reviewId, long reviewerId, string? comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 拒绝审查
    /// </summary>
    /// <param name="reviewId">审查ID</param>
    /// <param name="reviewerId">审查人ID</param>
    /// <param name="comment">审查意见</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RejectReviewAsync(long reviewId, long reviewerId, string? comment, CancellationToken cancellationToken = default);
}
