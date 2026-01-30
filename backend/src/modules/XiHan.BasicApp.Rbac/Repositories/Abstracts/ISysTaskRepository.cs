#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysTaskRepository
// Guid:d6e7f8a9-b0c1-2345-6789-012345d01234
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 任务调度仓储接口
/// </summary>
/// <remarks>
/// 聚合范围：SysTask + SysTaskLog
/// </remarks>
public interface ISysTaskRepository : IAggregateRootRepository<SysTask, long>
{
    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    Task<SysTask?> GetByTaskCodeAsync(string taskCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取启用的任务列表
    /// </summary>
    Task<List<SysTask>> GetActiveTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取需要执行的任务
    /// </summary>
    Task<List<SysTask>> GetPendingTasksAsync(DateTime currentTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务执行时间
    /// </summary>
    Task UpdateTaskExecutionAsync(long taskId, DateTime? lastExecuteTime, DateTime? nextExecuteTime, CancellationToken cancellationToken = default);

    // ========== 任务日志 ==========

    /// <summary>
    /// 添加任务日志
    /// </summary>
    Task<SysTaskLog> AddTaskLogAsync(SysTaskLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取任务日志列表
    /// </summary>
    Task<List<SysTaskLog>> GetTaskLogsAsync(long taskId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近任务日志
    /// </summary>
    Task<SysTaskLog?> GetLastTaskLogAsync(long taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期任务日志
    /// </summary>
    Task CleanExpiredLogsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
}
