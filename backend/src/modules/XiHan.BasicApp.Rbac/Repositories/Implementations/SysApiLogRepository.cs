#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysApiLogRepository
// Guid:b2b2c3d4-e5f6-7890-abcd-ef1234567891
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 系统API日志仓储实现
/// </summary>
public class SysApiLogRepository : SqlSugarRepositoryBase<SysApiLog, XiHanBasicAppIdType>, ISysApiLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysApiLogRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户ID获取API日志列表
    /// </summary>
    public async Task<List<SysApiLog>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        var result = await GetListAsync(log => log.UserId == userId);
        return result.OrderByDescending(log => log.RequestTime).ToList();
    }

    /// <summary>
    /// 根据API路径获取日志列表
    /// </summary>
    public async Task<List<SysApiLog>> GetByApiPathAsync(string apiPath)
    {
        var result = await GetListAsync(log => log.ApiPath == apiPath);
        return result.OrderByDescending(log => log.RequestTime).ToList();
    }

    /// <summary>
    /// 根据租户ID获取API日志列表
    /// </summary>
    public async Task<List<SysApiLog>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        var result = await GetListAsync(log => log.TenantId == tenantId);
        return result.OrderByDescending(log => log.RequestTime).ToList();
    }

    /// <summary>
    /// 根据时间范围获取API日志列表
    /// </summary>
    public async Task<List<SysApiLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var result = await GetListAsync(log => log.RequestTime >= startTime && log.RequestTime <= endTime);
        return result.OrderByDescending(log => log.RequestTime).ToList();
    }

    /// <summary>
    /// 根据状态码获取API日志列表
    /// </summary>
    public async Task<List<SysApiLog>> GetByStatusCodeAsync(int statusCode)
    {
        var result = await GetListAsync(log => log.StatusCode == statusCode);
        return result.OrderByDescending(log => log.RequestTime).ToList();
    }
}
