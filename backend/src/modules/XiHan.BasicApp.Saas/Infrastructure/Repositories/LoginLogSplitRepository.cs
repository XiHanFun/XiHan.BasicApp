#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginLogSplitRepository
// Guid:a1b2c3d4-0011-0004-0001-000000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 登录日志分表仓储实现
/// </summary>
public class LoginLogSplitRepository(ISqlSugarClientResolver clientResolver, ISplitTableLocator locator)
    : SqlSugarSplitRepository<SysLoginLog>(clientResolver, locator), ILoginLogSplitRepository
{
    public async Task<int> GetRecentFailureCountAsync(
        string userName,
        int minutes,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        if (minutes <= 0)
        {
            return 0;
        }

        var since = DateTimeOffset.UtcNow.AddMinutes(-minutes);
        var query = DbClient.Queryable<SysLoginLog>()
            .SplitTable()
            .Where(log => log.UserName == userName
                          && log.LoginTime >= since
                          && log.LoginResult != LoginResult.Success);

        if (tenantId.HasValue)
        {
            query = query.Where(log => log.TenantId == tenantId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<(List<SysLoginLog> Items, int Total)> GetPagedByUserIdAsync(
        long userId,
        long? tenantId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbClient.Queryable<SysLoginLog>()
            .SplitTable()
            .Where(log => log.UserId == userId);

        if (tenantId.HasValue)
        {
            query = query.Where(log => log.TenantId == tenantId.Value);
        }

        var total = new RefAsync<int>();
        var items = await query
            .OrderByDescending(log => log.LoginTime)
            .ToPageListAsync(pageIndex, pageSize, total, cancellationToken);

        return (items, total.Value);
    }
}
