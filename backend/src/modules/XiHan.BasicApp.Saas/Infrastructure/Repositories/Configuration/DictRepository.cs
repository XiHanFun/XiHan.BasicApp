#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictRepository
// Guid:7cb637f0-1f3c-42f3-ad50-34a158aab7b1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 字典仓储实现
/// </summary>
public sealed class DictRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysDict>(clientResolver), IDictRepository
{
    /// <inheritdoc />
    public async Task<SysDict?> GetByCodeAsync(string dictCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dictCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(dict => dict.DictCode == dictCode)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsCodeAsync(string dictCode, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dictCode);
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(dict => dict.DictCode == dictCode);
        if (excludeId.HasValue)
        {
            query = query.Where(dict => dict.BasicId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
