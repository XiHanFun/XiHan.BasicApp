#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOperationLogRepository
// Guid:b4c5d6e7-f8a9-4b5c-0d1e-3f4a5b6c7d8e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 操作日志仓储接口
/// </summary>
public interface IOperationLogRepository : IRepositoryBase<SysOperationLog, long>
{
    /// <summary>
    /// 根据用户ID获取操作日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志列表</returns>
    Task<List<SysOperationLog>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据操作类型获取操作日志
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志列表</returns>
    Task<List<SysOperationLog>> GetByOperationTypeAsync(string operationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间段内的操作日志
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志列表</returns>
    Task<List<SysOperationLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量插入操作日志
    /// </summary>
    /// <param name="logs">操作日志列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BatchInsertAsync(List<SysOperationLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理指定时间之前的日志
    /// </summary>
    /// <param name="beforeTime">截止时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理数量</returns>
    Task<int> CleanLogsBeforeAsync(DateTimeOffset beforeTime, CancellationToken cancellationToken = default);
}
