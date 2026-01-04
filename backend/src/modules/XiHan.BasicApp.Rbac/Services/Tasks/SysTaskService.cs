#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskService
// Guid:d1e2f3g4-h5i6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 16:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Tasks;
using XiHan.BasicApp.Rbac.Services.Tasks.Dtos;
using XiHan.Framework.Application.Services;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Services.Tasks;

/// <summary>
/// 系统任务服务实现
/// </summary>
public class SysTaskService : CrudApplicationServiceBase<SysTask, TaskDto, long, CreateTaskDto, UpdateTaskDto>, ISysTaskService
{
    private readonly ISysTaskRepository _taskRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysTaskService(ISysTaskRepository taskRepository) : base(taskRepository)
    {
        _taskRepository = taskRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    public async Task<TaskDto?> GetByTaskCodeAsync(string taskCode)
    {
        var task = await _taskRepository.GetByTaskCodeAsync(taskCode);
        return task?.Adapt<TaskDto>();
    }

    /// <summary>
    /// 根据任务状态获取任务列表
    /// </summary>
    public async Task<List<TaskDto>> GetByStatusAsync(TaskStatus taskStatus)
    {
        var tasks = await _taskRepository.GetByStatusAsync(taskStatus);
        return tasks.Adapt<List<TaskDto>>();
    }

    /// <summary>
    /// 获取待执行的任务列表
    /// </summary>
    public async Task<List<TaskDto>> GetPendingTasksAsync(int count = 100)
    {
        var tasks = await _taskRepository.GetPendingTasksAsync(count);
        return tasks.Adapt<List<TaskDto>>();
    }

    /// <summary>
    /// 根据任务分组获取任务列表
    /// </summary>
    public async Task<List<TaskDto>> GetByTaskGroupAsync(string taskGroup)
    {
        var tasks = await _taskRepository.GetByTaskGroupAsync(taskGroup);
        return tasks.Adapt<List<TaskDto>>();
    }

    /// <summary>
    /// 检查任务编码是否存在
    /// </summary>
    public async Task<bool> ExistsByTaskCodeAsync(string taskCode, long? excludeId = null)
    {
        return await _taskRepository.ExistsByTaskCodeAsync(taskCode, excludeId);
    }

    #endregion 业务特定方法
}
