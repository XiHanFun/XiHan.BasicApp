#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysTaskRepository
// Guid:a1b2c3d4-e5f6-7890-abcd-ef12345678a0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Repositories.Abstractions;

/// <summary>
/// 系统任务仓储接口
/// </summary>
public interface ISysTaskRepository : IRepositoryBase<SysTask, XiHanBasicAppIdType>
{
    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    /// <param name="taskCode">任务编码</param>
    /// <returns></returns>
    Task<SysTask?> GetByTaskCodeAsync(string taskCode);

    /// <summary>
    /// 根据任务状态获取任务列表
    /// </summary>
    /// <param name="taskStatus">任务状态</param>
    /// <returns></returns>
    Task<List<SysTask>> GetByStatusAsync(TaskStatus taskStatus);

    /// <summary>
    /// 获取待执行的任务列表
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns></returns>
    Task<List<SysTask>> GetPendingTasksAsync(int count = 100);

    /// <summary>
    /// 根据任务分组获取任务列表
    /// </summary>
    /// <param name="taskGroup">任务分组</param>
    /// <returns></returns>
    Task<List<SysTask>> GetByTaskGroupAsync(string taskGroup);

    /// <summary>
    /// 检查任务编码是否存在
    /// </summary>
    /// <param name="taskCode">任务编码</param>
    /// <param name="excludeId">排除的任务ID</param>
    /// <returns></returns>
    Task<bool> ExistsByTaskCodeAsync(string taskCode, XiHanBasicAppIdType? excludeId = null);
}
