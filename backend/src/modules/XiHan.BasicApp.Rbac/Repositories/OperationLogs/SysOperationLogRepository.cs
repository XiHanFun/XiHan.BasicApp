#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperationLogRepository
// Guid:b4b2c3d4-e5f6-7890-abcd-ef1234567893
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.OperationLogs;

/// <summary>
/// 系统操作日志仓储实现
/// </summary>
public class SysOperationLogRepository : SqlSugarRepositoryBase<SysOperationLog, long>, ISysOperationLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysOperationLogRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户ID获取操作日志列表
    /// </summary>
    public async Task<List<SysOperationLog>> GetByUserIdAsync(long userId)
    {
        var result = await GetListAsync(log => log.UserId == userId);
        return [.. result.OrderByDescending(log => log.OperationTime)];
    }

    /// <summary>
    /// 根据操作类型获取日志列表
    /// </summary>
    public async Task<List<SysOperationLog>> GetByOperationTypeAsync(OperationType operationType)
    {
        var result = await GetListAsync(log => log.OperationType == operationType);
        return [.. result.OrderByDescending(log => log.OperationTime)];
    }

    /// <summary>
    /// 根据租户ID获取操作日志列表
    /// </summary>
    public async Task<List<SysOperationLog>> GetByTenantIdAsync(long tenantId)
    {
        var result = await GetListAsync(log => log.TenantId == tenantId);
        return [.. result.OrderByDescending(log => log.OperationTime)];
    }

    /// <summary>
    /// 根据时间范围获取操作日志列表
    /// </summary>
    public async Task<List<SysOperationLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var result = await GetListAsync(log => log.OperationTime >= startTime && log.OperationTime <= endTime);
        return [.. result.OrderByDescending(log => log.OperationTime)];
    }

    /// <summary>
    /// 根据模块获取操作日志列表
    /// </summary>
    public async Task<List<SysOperationLog>> GetByModuleAsync(string module)
    {
        var result = await GetListAsync(log => log.Module == module);
        return [.. result.OrderByDescending(log => log.OperationTime)];
    }
}
