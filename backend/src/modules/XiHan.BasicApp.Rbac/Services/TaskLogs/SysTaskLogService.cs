#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskLogService
// Guid:k2c2d3e4-f5a6-7890-abcd-ef1234567913
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.TaskLogs;
using XiHan.BasicApp.Rbac.Services.TaskLogs.Dtos;
using XiHan.Framework.Application.Services;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Services.TaskLogs;

/// <summary>
/// 系统任务日志服务实现
/// </summary>
public class SysTaskLogService : CrudApplicationServiceBase<SysTaskLog, TaskLogDto, long, CreateTaskLogDto, CreateTaskLogDto>, ISysTaskLogService
{
    private readonly ISysTaskLogRepository _taskLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysTaskLogService(ISysTaskLogRepository taskLogRepository) : base(taskLogRepository)
    {
        _taskLogRepository = taskLogRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据任务ID获取任务日志列表
    /// </summary>
    public async Task<List<TaskLogDto>> GetByTaskIdAsync(long taskId)
    {
        var logs = await _taskLogRepository.GetByTaskIdAsync(taskId);
        return logs.Adapt<List<TaskLogDto>>();
    }

    /// <summary>
    /// 根据任务编码获取任务日志列表
    /// </summary>
    public async Task<List<TaskLogDto>> GetByTaskCodeAsync(string taskCode)
    {
        var logs = await _taskLogRepository.GetByTaskCodeAsync(taskCode);
        return logs.Adapt<List<TaskLogDto>>();
    }

    /// <summary>
    /// 根据任务状态获取任务日志列表
    /// </summary>
    public async Task<List<TaskLogDto>> GetByStatusAsync(TaskStatus taskStatus)
    {
        var logs = await _taskLogRepository.GetByStatusAsync(taskStatus);
        return logs.Adapt<List<TaskLogDto>>();
    }

    /// <summary>
    /// 根据时间范围获取任务日志列表
    /// </summary>
    public async Task<List<TaskLogDto>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _taskLogRepository.GetByTimeRangeAsync(startTime, endTime);
        return logs.Adapt<List<TaskLogDto>>();
    }

    /// <summary>
    /// 获取最近的任务日志
    /// </summary>
    public async Task<List<TaskLogDto>> GetRecentLogsAsync(long taskId, int count = 10)
    {
        var logs = await _taskLogRepository.GetRecentLogsAsync(taskId, count);
        return logs.Adapt<List<TaskLogDto>>();
    }

    #endregion 业务特定方法
}
