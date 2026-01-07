#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IApiLogRepository
// Guid:d6e7f8a9-b0c1-4d5e-2f3a-5b6c7d8e9f0a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// API日志仓储接口
/// </summary>
public interface IApiLogRepository : IRepositoryBase<SysApiLog, long>
{
    /// <summary>
    /// 根据用户ID获取API日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API日志列表</returns>
    Task<List<SysApiLog>> GetByUserIdAsync(long? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据API路径获取日志
    /// </summary>
    /// <param name="apiPath">API路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API日志列表</returns>
    Task<List<SysApiLog>> GetByApiPathAsync(string apiPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据HTTP状态码获取日志
    /// </summary>
    /// <param name="statusCode">HTTP状态码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API日志列表</returns>
    Task<List<SysApiLog>> GetByStatusCodeAsync(int statusCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间段内的API日志
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API日志列表</returns>
    Task<List<SysApiLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取慢查询日志（执行时间超过指定毫秒数）
    /// </summary>
    /// <param name="minDuration">最小执行时间（毫秒）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API日志列表</returns>
    Task<List<SysApiLog>> GetSlowLogsAsync(long minDuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量插入API日志
    /// </summary>
    /// <param name="logs">API日志列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BatchInsertAsync(List<SysApiLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理指定时间之前的日志
    /// </summary>
    /// <param name="beforeTime">截止时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理数量</returns>
    Task<int> CleanLogsBeforeAsync(DateTimeOffset beforeTime, CancellationToken cancellationToken = default);
}
