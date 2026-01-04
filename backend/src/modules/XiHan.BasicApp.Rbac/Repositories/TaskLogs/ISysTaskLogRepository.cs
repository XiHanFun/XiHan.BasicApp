#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysTaskLogRepository
// Guid:a2b2c3d4-e5f6-7890-abcd-ef12345678a1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Repositories.TaskLogs;

/// <summary>
/// 系统任务日志仓储接口
/// </summary>
public interface ISysTaskLogRepository : IRepositoryBase<SysTaskLog, long>
{
    /// <summary>
    /// 根据任务ID获取任务日志列表
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns></returns>
    Task<List<SysTaskLog>> GetByTaskIdAsync(long taskId);

    /// <summary>
    /// 根据任务编码获取任务日志列表
    /// </summary>
    /// <param name="taskCode">任务编码</param>
    /// <returns></returns>
    Task<List<SysTaskLog>> GetByTaskCodeAsync(string taskCode);

    /// <summary>
    /// 根据任务状态获取任务日志列表
    /// </summary>
    /// <param name="taskStatus">任务状态</param>
    /// <returns></returns>
    Task<List<SysTaskLog>> GetByStatusAsync(TaskStatus taskStatus);

    /// <summary>
    /// 根据时间范围获取任务日志列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    Task<List<SysTaskLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// 获取最近的任务日志
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="count">数量</param>
    /// <returns></returns>
    Task<List<SysTaskLog>> GetRecentLogsAsync(long taskId, int count = 10);
}
