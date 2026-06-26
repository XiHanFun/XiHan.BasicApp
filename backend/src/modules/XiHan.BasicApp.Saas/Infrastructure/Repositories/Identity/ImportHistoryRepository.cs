#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ImportHistoryRepository
// Guid:2a8b4e63-9c5d-4f0a-b2e7-6d3f1c0a9852
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 导入历史仓储实现
/// </summary>
public sealed class ImportHistoryRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysImportHistory>(clientResolver), IImportHistoryRepository
{
    /// <inheritdoc />
    public async Task<List<SysImportHistory>> GetRecentByUserAsync(long userId, string pageCode, int count, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(history => history.UserId == userId && history.PageCode == pageCode)
            .OrderByDescending(history => history.CreatedTime)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
