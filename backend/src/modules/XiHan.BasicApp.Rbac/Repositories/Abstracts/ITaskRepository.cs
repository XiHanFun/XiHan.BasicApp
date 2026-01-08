#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskRepository
// Guid:d0e1f2a3-b4c5-4d5e-6f7a-9b0c1d2e3f4a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 任务仓储接口
/// </summary>
public interface ITaskRepository : IAggregateRootRepository<SysTask, long>
{
    /// <summary>
    /// 根据任务编码查询任务
    /// </summary>
    /// <param name="taskCode">任务编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务实体</returns>
    Task<SysTask?> GetByTaskCodeAsync(string taskCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查任务编码是否存在
    /// </summary>
    /// <param name="taskCode">任务编码</param>
    /// <param name="excludeTaskId">排除的任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByTaskCodeAsync(string taskCode, long? excludeTaskId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据任务状态获取任务列表
    /// </summary>
    /// <param name="taskStatus">任务状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表</returns>
    Task<List<SysTask>> GetByTaskStatusAsync(TaskStatus taskStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取启用的任务列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表</returns>
    Task<List<SysTask>> GetActiveTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取需要执行的任务列表
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表</returns>
    Task<List<SysTask>> GetPendingTasksAsync(DateTimeOffset currentTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务执行信息
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="lastRunTime">最后运行时间</param>
    /// <param name="nextRunTime">下次运行时间</param>
    /// <param name="runCount">运行次数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateTaskRunInfoAsync(long taskId, DateTimeOffset lastRunTime, DateTimeOffset? nextRunTime, int runCount, CancellationToken cancellationToken = default);
}
