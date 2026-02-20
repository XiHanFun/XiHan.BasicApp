#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAccessLogRepository
// Guid:1c54b69f-9b7d-4f38-9255-7aa95f8f70d5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:32:10
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 访问日志仓储实现
/// </summary>
public class SysAccessLogRepository : SqlSugarReadOnlyRepository<SysAccessLog, long>, ISysAccessLogRepository
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

    /// <inheritdoc />
    public async Task<SysAccessLog> SaveAsync(SysAccessLog log, CancellationToken cancellationToken = default)
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
