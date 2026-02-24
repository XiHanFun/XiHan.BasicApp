#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysExceptionLogRepository
// Guid:d3fbdd5d-a05b-4de3-8b89-6db8f4255cc8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:32:50
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 异常日志仓储实现
/// </summary>
public class SysExceptionLogRepository : SqlSugarReadOnlyRepository<SysExceptionLog, long>, ISysExceptionLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysExceptionLogRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<SysExceptionLog> SaveAsync(SysExceptionLog log, CancellationToken cancellationToken = default)
    {
        if (log.IsTransient())
        {
            await _dbContext.GetClient().Insertable(log).SplitTable().ExecuteReturnSnowflakeIdAsync();
        }
        else
        {
            await _dbContext.GetClient().Updateable(log).SplitTable().ExecuteCommandAsync();
        }

        return log;
    }
}
