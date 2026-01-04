#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAccessLogRepository
// Guid:b1b2c3d4-e5f6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.AccessLogs;

/// <summary>
/// 系统访问日志仓储实现
/// </summary>
public class SysAccessLogRepository : SqlSugarRepositoryBase<SysAccessLog, long>, ISysAccessLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysAccessLogRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户ID获取访问日志列表
    /// </summary>
    public async Task<List<SysAccessLog>> GetByUserIdAsync(long userId)
    {
        var result = await GetListAsync(log => log.UserId == userId);
        return result.OrderByDescending(log => log.AccessTime).ToList();
    }

    /// <summary>
    /// 根据资源路径获取访问日志列表
    /// </summary>
    public async Task<List<SysAccessLog>> GetByResourcePathAsync(string resourcePath)
    {
        var result = await GetListAsync(log => log.ResourcePath == resourcePath);
        return result.OrderByDescending(log => log.AccessTime).ToList();
    }

    /// <summary>
    /// 根据租户ID获取访问日志列表
    /// </summary>
    public async Task<List<SysAccessLog>> GetByTenantIdAsync(long tenantId)
    {
        var result = await GetListAsync(log => log.TenantId == tenantId);
        return result.OrderByDescending(log => log.AccessTime).ToList();
    }

    /// <summary>
    /// 根据时间范围获取访问日志列表
    /// </summary>
    public async Task<List<SysAccessLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var result = await GetListAsync(log => log.AccessTime >= startTime && log.AccessTime <= endTime);
        return result.OrderByDescending(log => log.AccessTime).ToList();
    }
}
