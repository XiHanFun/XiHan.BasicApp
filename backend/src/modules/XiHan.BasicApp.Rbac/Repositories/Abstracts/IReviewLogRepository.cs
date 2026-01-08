#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IReviewLogRepository
// Guid:b728152c-d6e9-4396-addb-b479254bad81
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
/// 审查日志仓储接口
/// </summary>
public interface IReviewLogRepository : IRepositoryBase<SysReviewLog, long>
{
    /// <summary>
    /// 根据审查ID获取审查日志
    /// </summary>
    /// <param name="reviewId">审查ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查日志列表</returns>
    Task<List<SysReviewLog>> GetByReviewIdAsync(long reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据审查人ID获取审查日志
    /// </summary>
    /// <param name="reviewerId">审查人ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查日志列表</returns>
    Task<List<SysReviewLog>> GetByReviewerIdAsync(long reviewerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据审查结果获取审查日志
    /// </summary>
    /// <param name="reviewResult">审查结果</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查日志列表</returns>
    Task<List<SysReviewLog>> GetByReviewResultAsync(AuditResult reviewResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据审查类型获取审查日志
    /// </summary>
    /// <param name="reviewType">审查类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查日志列表</returns>
    Task<List<SysReviewLog>> GetByReviewTypeAsync(string reviewType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间段内的审查日志
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查日志列表</returns>
    Task<List<SysReviewLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量插入审查日志
    /// </summary>
    /// <param name="logs">审查日志列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BatchInsertAsync(List<SysReviewLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理指定时间之前的日志
    /// </summary>
    /// <param name="beforeTime">截止时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理数量</returns>
    Task<int> CleanLogsBeforeAsync(DateTimeOffset beforeTime, CancellationToken cancellationToken = default);
}
