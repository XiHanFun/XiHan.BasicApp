#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IReviewRepository
// Guid:a728152c-d6e9-4396-addb-b479254bad80
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 审查仓储接口
/// </summary>
public interface IReviewRepository : IAggregateRootRepository<SysReview, long>
{
    /// <summary>
    /// 根据审查编码查询审查
    /// </summary>
    /// <param name="reviewCode">审查编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查实体</returns>
    Task<SysReview?> GetByReviewCodeAsync(string reviewCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查审查编码是否存在
    /// </summary>
    /// <param name="reviewCode">审查编码</param>
    /// <param name="excludeReviewId">排除的审查ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByReviewCodeAsync(string reviewCode, long? excludeReviewId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据审查状态获取审查列表
    /// </summary>
    /// <param name="reviewStatus">审查状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查列表</returns>
    Task<List<SysReview>> GetByReviewStatusAsync(AuditStatus reviewStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据审查类型获取审查列表
    /// </summary>
    /// <param name="reviewType">审查类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查列表</returns>
    Task<List<SysReview>> GetByReviewTypeAsync(string reviewType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据提交人ID获取审查列表
    /// </summary>
    /// <param name="submitterId">提交人ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查列表</returns>
    Task<List<SysReview>> GetBySubmitterIdAsync(long submitterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据当前审查人ID获取审查列表
    /// </summary>
    /// <param name="reviewerId">审查人ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查列表</returns>
    Task<List<SysReview>> GetByCurrentReviewerIdAsync(long reviewerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据业务实体获取审查列表
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查列表</returns>
    Task<List<SysReview>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待审查列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查列表</returns>
    Task<List<SysReview>> GetPendingReviewsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间段内的审查列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查列表</returns>
    Task<List<SysReview>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新审查状态
    /// </summary>
    /// <param name="reviewId">审查ID</param>
    /// <param name="reviewStatus">审查状态</param>
    /// <param name="reviewResult">审查结果</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateReviewStatusAsync(long reviewId, AuditStatus reviewStatus, AuditResult? reviewResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新当前审查人
    /// </summary>
    /// <param name="reviewId">审查ID</param>
    /// <param name="reviewerId">审查人ID</param>
    /// <param name="reviewerName">审查人名称</param>
    /// <param name="currentLevel">当前级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateCurrentReviewerAsync(long reviewId, long reviewerId, string reviewerName, int currentLevel, CancellationToken cancellationToken = default);
}
