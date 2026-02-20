#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditLogRepository
// Guid:076a08f7-6f39-4f69-9ce4-b0cbb8637699
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:33:10
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 审计日志仓储实现
/// </summary>
public class SysAuditLogRepository : SqlSugarReadOnlyRepository<SysAuditLog, long>, ISysAuditLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysAuditLogRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<SysAuditLog> SaveAsync(SysAuditLog log, CancellationToken cancellationToken = default)
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
