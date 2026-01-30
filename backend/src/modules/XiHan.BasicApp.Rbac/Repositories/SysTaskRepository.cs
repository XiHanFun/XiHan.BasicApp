#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskRepository
// Guid:d4e5f6a7-b8c9-0123-4567-890123d89012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 任务调度仓储实现
/// </summary>
public class SysTaskRepository : SqlSugarAggregateRepository<SysTask, long>, ISysTaskRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysTaskRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    public async Task<SysTask?> GetByTaskCodeAsync(string taskCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysTask>()
            .Where(t => t.TaskCode == taskCode && t.Status == YesOrNo.Yes)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取启用的任务列表
    /// </summary>
    public async Task<List<SysTask>> GetActiveTasksAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysTask>()
            .Where(t => t.Status == YesOrNo.Yes && t.RunTaskStatus == RunTaskStatus.Running)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取需要执行的任务
    /// </summary>
    public async Task<List<SysTask>> GetPendingTasksAsync(DateTime currentTime, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysTask>()
            .Where(t => t.Status == YesOrNo.Yes
                && t.RunTaskStatus == RunTaskStatus.Running
                && (t.NextExecuteTime == null || t.NextExecuteTime <= currentTime))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 更新任务执行时间
    /// </summary>
    public async Task UpdateTaskExecutionAsync(long taskId, DateTime? lastExecuteTime, DateTime? nextExecuteTime, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysTask>()
            .SetColumns(t => new SysTask
            {
                LastExecuteTime = lastExecuteTime,
                NextExecuteTime = nextExecuteTime
            })
            .Where(t => t.BasicId == taskId)
            .ExecuteCommandAsync(cancellationToken);
    }

    // ========== 任务日志 ==========

    /// <summary>
    /// 添加任务日志
    /// </summary>
    public async Task<SysTaskLog> AddTaskLogAsync(SysTaskLog log, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(log).ExecuteReturnEntityAsync(cancellationToken);
    }

    /// <summary>
    /// 获取任务日志列表
    /// </summary>
    public async Task<List<SysTaskLog>> GetTaskLogsAsync(long taskId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysTaskLog>()
            .Where(log => log.TaskId == taskId)
            .OrderByDescending(log => log.ExecuteTime)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);
    }

    /// <summary>
    /// 获取最近任务日志
    /// </summary>
    public async Task<SysTaskLog?> GetLastTaskLogAsync(long taskId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysTaskLog>()
            .Where(log => log.TaskId == taskId)
            .OrderByDescending(log => log.ExecuteTime)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 清理过期任务日志
    /// </summary>
    public async Task CleanExpiredLogsAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysTaskLog>()
            .Where(log => log.ExecuteTime < beforeDate)
            .ExecuteCommandAsync(cancellationToken);
    }
}
