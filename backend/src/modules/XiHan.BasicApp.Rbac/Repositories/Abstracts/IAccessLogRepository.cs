#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAccessLogRepository
// Guid:e7f8a9b0-c1d2-4e5f-3a4b-6c7d8e9f0a1b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 访问日志仓储接口
/// </summary>
public interface IAccessLogRepository : IRepositoryBase<SysAccessLog, long>
{
    /// <summary>
    /// 根据用户ID获取访问日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问日志列表</returns>
    Task<List<SysAccessLog>> GetByUserIdAsync(long? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据访问路径获取日志
    /// </summary>
    /// <param name="resourcePath">访问路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问日志列表</returns>
    Task<List<SysAccessLog>> GetByResourcePathAsync(string resourcePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据IP地址获取访问日志
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问日志列表</returns>
    Task<List<SysAccessLog>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间段内的访问日志
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问日志列表</returns>
    Task<List<SysAccessLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计访问量
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问量</returns>
    Task<int> CountAccessAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量插入访问日志
    /// </summary>
    /// <param name="logs">访问日志列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BatchInsertAsync(List<SysAccessLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理指定时间之前的日志
    /// </summary>
    /// <param name="beforeTime">截止时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理数量</returns>
    Task<int> CleanLogsBeforeAsync(DateTimeOffset beforeTime, CancellationToken cancellationToken = default);
}
