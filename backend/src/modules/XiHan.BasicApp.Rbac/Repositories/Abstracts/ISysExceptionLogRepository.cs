#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysExceptionLogRepository
// Guid:e1f2a3b4-c5d6-7890-1234-567890e56789
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 异常日志仓储接口
/// </summary>
public interface ISysExceptionLogRepository : IRepositoryBase<SysExceptionLog, long>
{
    /// <summary>
    /// 添加异常日志
    /// </summary>
    Task<SysExceptionLog> AddExceptionLogAsync(SysExceptionLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加异常日志
    /// </summary>
    Task AddExceptionLogsAsync(IEnumerable<SysExceptionLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近异常日志
    /// </summary>
    Task<List<SysExceptionLog>> GetRecentExceptionsAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取异常统计
    /// </summary>
    Task<Dictionary<string, int>> GetExceptionStatisticsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按异常类型统计
    /// </summary>
    Task<Dictionary<string, int>> GetExceptionTypeStatisticsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}
