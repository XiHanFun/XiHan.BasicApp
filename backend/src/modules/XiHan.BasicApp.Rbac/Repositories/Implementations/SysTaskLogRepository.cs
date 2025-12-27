#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskLogRepository
// Guid:c2b2c3d4-e5f6-7890-abcd-ef12345678a1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 系统任务日志仓储实现
/// </summary>
public class SysTaskLogRepository : SqlSugarRepositoryBase<SysTaskLog, XiHanBasicAppIdType>, ISysTaskLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysTaskLogRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据任务ID获取任务日志列表
    /// </summary>
    public async Task<List<SysTaskLog>> GetByTaskIdAsync(XiHanBasicAppIdType taskId)
    {
        var result = await GetListAsync(log => log.TaskId == taskId);
        return result.OrderByDescending(log => log.StartTime).ToList();
    }

    /// <summary>
    /// 根据任务编码获取任务日志列表
    /// </summary>
    public async Task<List<SysTaskLog>> GetByTaskCodeAsync(string taskCode)
    {
        var result = await GetListAsync(log => log.TaskCode == taskCode);
        return result.OrderByDescending(log => log.StartTime).ToList();
    }

    /// <summary>
    /// 根据任务状态获取任务日志列表
    /// </summary>
    public async Task<List<SysTaskLog>> GetByStatusAsync(TaskStatus taskStatus)
    {
        var result = await GetListAsync(log => log.TaskStatus == taskStatus);
        return result.OrderByDescending(log => log.StartTime).ToList();
    }

    /// <summary>
    /// 根据时间范围获取任务日志列表
    /// </summary>
    public async Task<List<SysTaskLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var result = await GetListAsync(log => log.StartTime >= startTime && log.StartTime <= endTime);
        return result.OrderByDescending(log => log.StartTime).ToList();
    }

    /// <summary>
    /// 获取最近的任务日志
    /// </summary>
    public async Task<List<SysTaskLog>> GetRecentLogsAsync(XiHanBasicAppIdType taskId, int count = 10)
    {
        return await _dbContext.GetClient()
            .Queryable<SysTaskLog>()
            .Where(log => log.TaskId == taskId)
            .OrderByDescending(log => log.StartTime)
            .Take(count)
            .ToListAsync();
    }
}
