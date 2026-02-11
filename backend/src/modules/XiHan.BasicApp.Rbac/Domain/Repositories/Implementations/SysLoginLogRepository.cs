#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysLoginLogRepository
// Guid:a1b2c3d4-e5f6-7890-1234-567890abcdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 系统登录日志仓储实现
/// </summary>
public class SysLoginLogRepository : SqlSugarReadOnlyRepository<SysLoginLog, long>, ISysLoginLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysLoginLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户ID获取登录日志
    /// </summary>
    public async Task<List<SysLoginLog>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysLoginLog>()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.LoginTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 保存登录日志
    /// </summary>
    public async Task<SysLoginLog> SaveAsync(SysLoginLog loginLog, CancellationToken cancellationToken = default)
    {
        if (loginLog.BasicId == 0)
        {
            // 新增
            await _dbContext.GetClient().Insertable(loginLog).ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            // 更新
            await _dbContext.GetClient().Updateable(loginLog).ExecuteCommandAsync(cancellationToken);
        }

        return loginLog;
    }

    /// <summary>
    /// 获取最近的登录失败记录数
    /// </summary>
    public async Task<int> GetRecentFailureCountAsync(string userName, int minutes = 30, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.Now.AddMinutes(-minutes);

        return await _dbContext.GetClient().Queryable<SysLoginLog>()
            .Where(log => log.UserName == userName
                && log.LoginTime >= startTime
                && log.LoginResult != LoginResult.Success)
            .CountAsync(cancellationToken);
    }
}
