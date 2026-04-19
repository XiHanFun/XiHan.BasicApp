#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskLogRepository
// Guid:813588ef-ac6f-45cc-a865-a946d763eb22
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 11:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 任务日志仓储实现
/// </summary>
public class TaskLogRepository : SqlSugarRepositoryBase<SysTaskLog, long>, ITaskLogRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskLogRepository(
        ISqlSugarClientResolver clientResolver)
        : base(clientResolver)
    {
    }

    /// <summary>
    /// 清空任务日志
    /// </summary>
    public async Task<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var affectedRows = await DbClient.Deleteable<SysTaskLog>()
            .SplitTable(tables => tables)
            .ExecuteCommandAsync();
        return affectedRows > 0;
    }
}
