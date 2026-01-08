#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskRepository
// Guid:d0e1f2a3-b4c5-4d5e-6f7a-9b0c1d2e3f4a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 任务仓储实现
/// </summary>
public class TaskRepository : SqlSugarAggregateRepository<SysTask, long>, ITaskRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据任务编码查询任务
    /// </summary>
    public async Task<SysTask?> GetByTaskCodeAsync(string taskCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysTask>()
            .FirstAsync(t => t.TaskCode == taskCode, cancellationToken);
    }

    /// <summary>
    /// 检查任务编码是否存在
    /// </summary>
    public async Task<bool> ExistsByTaskCodeAsync(string taskCode, long? excludeTaskId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysTask>()
            .Where(t => t.TaskCode == taskCode);

        if (excludeTaskId.HasValue)
        {
            query = query.Where(t => t.BasicId != excludeTaskId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 根据任务状态获取任务列表
    /// </summary>
    public async Task<List<SysTask>> GetByTaskStatusAsync(TaskStatus taskStatus, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysTask>()
            .Where(t => t.TaskStatus == taskStatus)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取启用的任务列表
    /// </summary>
    public async Task<List<SysTask>> GetActiveTasksAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysTask>()
            .Where(t => t.Status == YesOrNo.Yes)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取需要执行的任务列表
    /// </summary>
    public async Task<List<SysTask>> GetPendingTasksAsync(DateTimeOffset currentTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysTask>()
            .Where(t => t.Status == YesOrNo.Yes
                && t.TaskStatus == TaskStatus.Pending
                && (t.NextRunTime == null || t.NextRunTime <= currentTime))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 更新任务执行信息
    /// </summary>
    public async Task<bool> UpdateTaskRunInfoAsync(long taskId, DateTimeOffset lastRunTime, DateTimeOffset? nextRunTime, int runCount, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysTask>()
            .SetColumns(t => new SysTask
            {
                LastRunTime = lastRunTime,
                NextRunTime = nextRunTime,
                ExecutedCount = runCount
            })
            .Where(t => t.BasicId == taskId)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }
}
