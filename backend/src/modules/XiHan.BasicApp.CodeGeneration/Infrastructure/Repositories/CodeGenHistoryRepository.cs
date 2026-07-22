// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Repositories;

/// <summary>
/// 代码生成历史仓储实现
/// </summary>
public sealed class CodeGenHistoryRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysCodeGenHistory>(clientResolver), ICodeGenHistoryRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysCodeGenHistory>> GetByTableIdAsync(long tableId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(history => history.TableId == tableId)
            .OrderBy(history => history.GenTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysCodeGenHistory?> GetByBatchNumberAsync(string batchNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchNumber);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(history => history.BatchNumber == batchNumber.Trim())
            .FirstAsync(cancellationToken);
    }
}
