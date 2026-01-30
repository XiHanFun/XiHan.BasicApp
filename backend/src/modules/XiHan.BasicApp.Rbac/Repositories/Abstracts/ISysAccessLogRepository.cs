#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysAccessLogRepository
// Guid:c9d0e1f2-a3b4-5678-9012-345678c34567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 访问与安全日志仓储接口
/// </summary>
/// <remarks>
/// 覆盖实体：SysAccessLog + SysLoginLog + SysApiLog
/// </remarks>
public interface ISysAccessLogRepository : IRepositoryBase<SysAccessLog, long>
{
    // ========== 访问日志 ==========

    /// <summary>
    /// 批量添加访问日志
    /// </summary>
    Task AddAccessLogsAsync(IEnumerable<SysAccessLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户访问日志
    /// </summary>
    Task<List<SysAccessLog>> GetByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    // ========== 登录日志 ==========

    /// <summary>
    /// 添加登录日志
    /// </summary>
    Task<SysLoginLog> AddLoginLogAsync(SysLoginLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户登录日志
    /// </summary>
    Task<List<SysLoginLog>> GetLoginLogsByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近登录日志
    /// </summary>
    Task<SysLoginLog?> GetLastLoginLogAsync(long userId, CancellationToken cancellationToken = default);

    // ========== API日志 ==========

    /// <summary>
    /// 添加API日志
    /// </summary>
    Task<SysApiLog> AddApiLogAsync(SysApiLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加API日志
    /// </summary>
    Task AddApiLogsAsync(IEnumerable<SysApiLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取API调用统计
    /// </summary>
    Task<Dictionary<string, int>> GetApiCallStatisticsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}
