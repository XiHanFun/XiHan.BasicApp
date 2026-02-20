#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperationLogRepository
// Guid:8649831b-f413-4cc2-bdf9-5e78f648b352
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:32:30
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 操作日志仓储实现
/// </summary>
public class SysOperationLogRepository : SqlSugarReadOnlyRepository<SysOperationLog, long>, ISysOperationLogRepository
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

    /// <inheritdoc />
    public async Task<SysOperationLog> SaveAsync(SysOperationLog log, CancellationToken cancellationToken = default)
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
