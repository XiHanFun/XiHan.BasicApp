#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskRepository
// Guid:c1b2c3d4-e5f6-7890-abcd-ef12345678a0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Repositories.Tasks;

/// <summary>
/// 系统任务仓储实现
/// </summary>
public class SysTaskRepository : SqlSugarRepositoryBase<SysTask, XiHanBasicAppIdType>, ISysTaskRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysTaskRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    public async Task<SysTask?> GetByTaskCodeAsync(string taskCode)
    {
        return await GetFirstAsync(task => task.TaskCode == taskCode);
    }

    /// <summary>
    /// 根据任务状态获取任务列表
    /// </summary>
    public async Task<List<SysTask>> GetByStatusAsync(TaskStatus taskStatus)
    {
        var result = await GetListAsync(task => task.TaskStatus == taskStatus);
        return result.OrderBy(task => task.Priority).ThenBy(task => task.NextRunTime).ToList();
    }

    /// <summary>
    /// 获取待执行的任务列表
    /// </summary>
    public async Task<List<SysTask>> GetPendingTasksAsync(int count = 100)
    {
        return await _dbContext.GetClient()
            .Queryable<SysTask>()
            .Where(task => task.TaskStatus == TaskStatus.Pending && task.NextRunTime <= DateTimeOffset.Now)
            .OrderBy(task => task.Priority)
            .OrderBy(task => task.NextRunTime)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// 根据任务分组获取任务列表
    /// </summary>
    public async Task<List<SysTask>> GetByTaskGroupAsync(string taskGroup)
    {
        var result = await GetListAsync(task => task.TaskGroup == taskGroup);
        return result.OrderBy(task => task.Priority).ToList();
    }

    /// <summary>
    /// 检查任务编码是否存在
    /// </summary>
    public async Task<bool> ExistsByTaskCodeAsync(string taskCode, XiHanBasicAppIdType? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysTask>().Where(task => task.TaskCode == taskCode);
        if (excludeId.HasValue)
        {
            query = query.Where(task => task.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }
}
