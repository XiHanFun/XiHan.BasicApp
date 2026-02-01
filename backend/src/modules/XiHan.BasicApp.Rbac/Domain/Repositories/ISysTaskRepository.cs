#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysTaskRepository
// Guid:e5f6a7b8-c9d0-1234-5678-90abcdef1234
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统任务仓储接口
/// </summary>
public interface ISysTaskRepository : IAggregateRootRepository<SysTask, long>
{
    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    /// <param name="taskCode">任务编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务实体</returns>
    Task<SysTask?> GetByTaskCodeAsync(string taskCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查任务编码是否存在
    /// </summary>
    /// <param name="taskCode">任务编码</param>
    /// <param name="excludeTaskId">排除的任务ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsTaskCodeExistsAsync(string taskCode, long? excludeTaskId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待执行的任务列表
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表</returns>
    Task<List<SysTask>> GetPendingTasksAsync(DateTimeOffset currentTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户下的任务列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表</returns>
    Task<List<SysTask>> GetTasksByTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存任务
    /// </summary>
    /// <param name="task">任务实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的任务实体</returns>
    Task<SysTask> SaveAsync(SysTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task StartTaskAsync(long taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 暂停任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PauseTaskAsync(long taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task StopTaskAsync(long taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务执行信息
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="lastRunTime">上次执行时间</param>
    /// <param name="nextRunTime">下次执行时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateExecutionInfoAsync(long taskId, DateTimeOffset lastRunTime, DateTimeOffset? nextRunTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveAsync(long taskId, CancellationToken cancellationToken = default);
}
